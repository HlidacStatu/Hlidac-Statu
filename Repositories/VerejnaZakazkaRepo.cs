using HlidacStatu.Entities.VZ;
using HlidacStatu.Util;

using Nest;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.RateLimiting;
using System.Threading.Tasks;
using Devmasters;
using Devmasters.Collections;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using Polly;
using HlidacStatu.DS.Api;
using Polly.RateLimiting;

namespace HlidacStatu.Repositories
{
    public static partial class VerejnaZakazkaRepo
    {
        // private static AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = HttpPolicyExtensions
        //     .HandleTransientHttpError()
        //     .OrResult(r => r.RequestMessage.RequestUri.Host.Contains("bpapi.datlab.eu") &&
        //                    r.StatusCode == HttpStatusCode.Forbidden)
        //     .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(1));

        // private static ResiliencePipeline<HttpResponseMessage> _retryPipeline;
        // private static ResiliencePipeline<HttpResponseMessage> _retryThrottledPipeline;
        private static ConcurrentDictionary<string,ResiliencePipeline<HttpResponseMessage>> _retryThrottledPipelinePerHost = new ();

        // Emergency HttpClient for cases where it is not convinient to inject HttpClient.
        private static Lazy<HttpClient> _lazyHttpClient = new();

        // static VerejnaZakazkaRepo()
        // {
        //     _retryPipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
        //         .AddRetry(new()
        //         {
        //             ShouldHandle = static args => args.Outcome switch
        //             {
        //                 { Exception: HttpRequestException } or { Exception: SocketException} => PredicateResult.True(),
        //                 { Result.StatusCode: >= HttpStatusCode.InternalServerError } => PredicateResult.True(),
        //                 { Result.StatusCode: HttpStatusCode.RequestTimeout } => PredicateResult.True(),
        //                 var outcome when outcome.Result?.StatusCode == HttpStatusCode.Forbidden 
        //                                  && outcome.Result?.RequestMessage?.RequestUri?.Host.Contains("bpapi.datlab.eu") == true => PredicateResult.True(),
        //                 _ => PredicateResult.False()
        //             },
        //             MaxRetryAttempts = 3,
        //             Delay = TimeSpan.FromSeconds(2),
        //             UseJitter = true
        //         })
        //         .Build();
        //     
        // }

        private static ResiliencePipeline<HttpResponseMessage> CreateNewResiliencePipeline()
        {
            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                .AddConcurrencyLimiter(new ConcurrencyLimiterOptions()
                {
                    PermitLimit = 3,
                    QueueLimit = 10000
                })
                .AddRetry(new()
                {
                    ShouldHandle = static args => args.Outcome switch
                    {
                        { Exception: HttpRequestException } or { Exception: SocketException} => PredicateResult.True(),
                        { Result.StatusCode: >= HttpStatusCode.InternalServerError } => PredicateResult.True(),
                        { Result.StatusCode: HttpStatusCode.RequestTimeout } => PredicateResult.True(),
                        _ => PredicateResult.False()
                    },
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    UseJitter = true
                })
                .Build();
        }
        
        private static void AfterSave(VerejnaZakazka vz)
        {
            RecalculateItemRepo.AddFirmaToProcessingQueue(vz.Zadavatel.ICO, RecalculateItem.StatisticsTypeEnum.VZ , $"VZ {vz.Id}");
            foreach (var dod in vz.Dodavatele)
                _ = RecalculateItemRepo.AddFirmaToProcessingQueue(dod.ICO, RecalculateItem.StatisticsTypeEnum.VZ, $"VZ {vz.Id}");
        }

        private static async Task SaveIncompleteVzAsync(VerejnaZakazka incompleteVz, ElasticClient elasticClient)
        {
            try
            {
                //teoreticky můžeme použít incompleteVz.Changelog na zapsání nalezených chyb
                elasticClient ??= Manager.GetESClient_VerejneZakazky();
                incompleteVz.HasIssues = true;
                await elasticClient.IndexDocumentAsync<VerejnaZakazka>(incompleteVz);
                AfterSave(incompleteVz);

            }
            catch (Exception e)
            {
                _logger.Error(e, $"VZ ERROR Upserting ID:{incompleteVz.Id} Size:{Newtonsoft.Json.JsonConvert.SerializeObject(incompleteVz).Length}");
            }
        }

        /// <summary>
        /// Update or insert new
        /// </summary>
        public static async Task UpsertAsync(VerejnaZakazka newVZ, ElasticClient elasticClient = null,
            HttpClient httpClient = null, DateTime? posledniZmena = null, bool sendToOcr = true, bool updatePosledniZmena = true,
            bool shouldDownloadFile = true)
        {
            if (newVZ is null)
                return;

            if (httpClient is null)
                httpClient = _lazyHttpClient.Value;
            
            try
            {
                
                // try to find Ico
                if (newVZ.Zadavatel.ICO is null)
                {
                    var firma = FirmaRepo.FromName(newVZ.Zadavatel.Jmeno);
                    if(firma is not null)
                        newVZ.Zadavatel.ICO = firma.ICO;
                }
                
                // there might be issues to find proper ICO, so we have to save half-complete VZ
                if (string.IsNullOrWhiteSpace(newVZ.Zadavatel?.ICO))
                {
                    FixOldUrl(newVZ);
                    await SaveIncompleteVzAsync(newVZ, elasticClient);
                    return;
                }
                
                elasticClient ??= Manager.GetESClient_VerejneZakazky();
                var storedDuplicates = await FindDocumentsFromTheSameSourcesAsync(newVZ, elasticClient);

                // VZ existuje => mergujeme
                if (storedDuplicates is not null && storedDuplicates.Any())
                {
                    var storedDuplicatesOrdered = storedDuplicates.OrderBy(d => d.LastUpdated).ToList();

                    //the oldest one will be our permanent doc 
                    var firstVz = storedDuplicatesOrdered.First();

                    // firstly merge all connected VZs in db
                    foreach (var storedDuplicate in storedDuplicatesOrdered.Skip(1))
                    {
                        firstVz = MergeVz(firstVz, storedDuplicate);
                        //merge dokumenty
                        MergeDocuments(firstVz, storedDuplicate.Dokumenty);

                        try
                        {
                            await elasticClient.DeleteByQueryAsync<VerejnaZakazka>(s => s.Query(q =>
                                q.Term(t => t.Field(f => f.Id).Value(storedDuplicate.Id))));
                        }
                        catch (Exception e)
                        {
                            _logger.Error(e, $"VZ ERROR Merging ID:{firstVz.Id} with ID:{storedDuplicate.Id}.");
                        }
                    }

                    firstVz = MergeVz(firstVz, newVZ); //počítá se s tím, že jsou dokumenty stažené
                    MergeDocuments(firstVz, newVZ.Dokumenty);

                    newVZ = firstVz;
                }
                
                // stáhnout dokumenty, které nemají checksum
                if (shouldDownloadFile)
                {
                    await StoreDocumentCopyToHlidacStorageAsync(newVZ, httpClient);
                }
                SetupUpdateDates(newVZ, posledniZmena, updatePosledniZmena);
                
                // zmergovat stejné dokumenty podle sha
                MergeDocumentsBySHA(newVZ);
                
                if (sendToOcr)
                    SendToOcrQueue(newVZ);

                FixPublicationDate(newVZ);
                FixOldUrl(newVZ);
                await elasticClient.IndexDocumentAsync<VerejnaZakazka>(newVZ);
                AfterSave(newVZ);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"VZ ERROR Upserting ID:{newVZ.Id} Size:{Newtonsoft.Json.JsonConvert.SerializeObject(newVZ).Length}");
            }
        }

        private static void FixOldUrl(VerejnaZakazka newVz)
        {
            newVz.UrlZakazky = newVz.UrlZakazky
                .Select(u => u.Replace("https://www.vestnikverejnychzakazek.cz/SearchForm/SearchContract?contractNumber=", 
                    "https://vvz.nipez.cz/formulare-zakazky/"))
                .ToHashSet();
        }

        public static async Task RedownloadVzFiles(VerejnaZakazka vz, ElasticClient elasticClient, HttpClient httpClient)
        {
            if (vz is null)
                return;
            
            try
            {
                // stáhnout dokumenty, které nemají checksum
                await StoreDocumentCopyToHlidacStorageAsync(vz, httpClient);
                MergeDocumentsBySHA(vz);
                SendToOcrQueue(vz);
                
                await elasticClient.IndexDocumentAsync<VerejnaZakazka>(vz);
            }
            catch (Exception e)
            {
                _logger.Error(e, $"VZ Redownload ID:{vz.Id}");
            }
        }

        public static void FixPublicationDate(VerejnaZakazka firstVz)
        {
            if (firstVz.DatumUverejneni is null && firstVz.FirstPublishedDocument() != null)
            {
                firstVz.DatumUverejneni = firstVz.FirstPublishedDocument()?.VlozenoNaProfil;
            }
        }

        private static VerejnaZakazka MergeVz(VerejnaZakazka destination, VerejnaZakazka source)
        {
            MergeSimpleProperties(ref destination, source);

            // merge dodavatele
            foreach (var dodavatel in source.Dodavatele)
            {
                var origDodavatel = destination.Dodavatele.FirstOrDefault(d => d.Equals(dodavatel));

                if (origDodavatel is not null)
                {
                    //update
                    origDodavatel.Jmeno =
                        SetProperty(origDodavatel.Jmeno, dodavatel.Jmeno, "Dodavatel.Jmeno", destination.Changelog);
                }
                else
                {
                    //add
                    destination.Dodavatele.Add(dodavatel);
                }
            }
            return destination;
        }

        /// <summary>
        /// Merge newDocuments into originalVZ.Dokumenty
        /// </summary>
        /// <param name="originalVZ"></param>
        /// <param name="newDocuments"></param>
        private static void MergeDocuments(VerejnaZakazka originalVZ, List<VerejnaZakazka.Document> newDocuments)
        {
            foreach (var newDoc in newDocuments)
            {
                // update document by checksum
                var origDoc = originalVZ.Dokumenty.FirstOrDefault(d => d.Equals(newDoc)) 
                              // update document by link
                              ?? originalVZ.Dokumenty.FirstOrDefault(d => d.DirectUrls.Any() && d.DirectUrls.Overlaps(newDoc.DirectUrls))
                              // update document by storage id
                              ?? originalVZ.Dokumenty.FirstOrDefault(d => d.StorageIds.Any() && d.StorageIds.Overlaps(newDoc.StorageIds));

                if (origDoc is not null)
                {
                    MergeDocumentProperties(origDoc, newDoc, originalVZ.Changelog);
                    continue;
                }

                // add missing one
                originalVZ.Dokumenty.Add(newDoc);
            }
        }

        private static void MergeDocumentsBySHA(VerejnaZakazka newVZ)
        {
            var groups = newVZ.Dokumenty
                .Where(d => !string.IsNullOrWhiteSpace(d.Sha256Checksum))
                .GroupBy(d => d.Sha256Checksum)
                .Where(g => g.Count() > 1);
            foreach (var group in groups)
            {
                var main = group.FirstOrDefault();
                foreach (var duplicate in group.Skip(1))
                {
                    MergeDocumentProperties(main, duplicate, newVZ.Changelog);
                }
            }
        }

        public static async Task UpdateDocumentsInVz(string id, List<VerejnaZakazka.Document> dokumenty, ElasticClient elasticClient = null)
        {
            elasticClient ??= Manager.GetESClient_VerejneZakazky();
            var zakazka = await LoadFromESAsync(id, elasticClient);
            MergeDocuments(zakazka, dokumenty);
            await elasticClient.IndexDocumentAsync<VerejnaZakazka>(zakazka);
        }

        private static void MergeSimpleProperties(ref VerejnaZakazka originalVZ, VerejnaZakazka newVZ)
        {
            if (newVZ.Changelog.Count > 0)
            {
                originalVZ.Changelog.Add("Merging changelogs {{");
                originalVZ.Changelog.AddRange(newVZ.Changelog);
                originalVZ.Changelog.Add("}} Merging changelogs");
            }
            
            // preferujeme nové informace před starými
            originalVZ.PosledniZmena = SetProperty(originalVZ.PosledniZmena, newVZ.PosledniZmena,
                nameof(originalVZ.PosledniZmena), originalVZ.Changelog);
            originalVZ.LastUpdated = SetProperty(originalVZ.LastUpdated, newVZ.LastUpdated,
                nameof(originalVZ.LastUpdated), originalVZ.Changelog);
            
            originalVZ.DatumUverejneni = SetProperty(originalVZ.DatumUverejneni, newVZ.DatumUverejneni,
                nameof(originalVZ.DatumUverejneni), originalVZ.Changelog);
            originalVZ.DatumUzavreniSmlouvy = SetProperty(originalVZ.DatumUzavreniSmlouvy, newVZ.DatumUzavreniSmlouvy,
                nameof(originalVZ.DatumUzavreniSmlouvy), originalVZ.Changelog);
            originalVZ.LhutaDoruceni = SetProperty(originalVZ.LhutaDoruceni, newVZ.LhutaDoruceni,
                nameof(originalVZ.LhutaDoruceni), originalVZ.Changelog);
            originalVZ.LhutaPrihlaseni = SetProperty(originalVZ.LhutaPrihlaseni, newVZ.LhutaPrihlaseni,
                nameof(originalVZ.LhutaPrihlaseni), originalVZ.Changelog);
            originalVZ.NazevZakazky = SetProperty(originalVZ.NazevZakazky, newVZ.NazevZakazky,
                nameof(originalVZ.NazevZakazky), originalVZ.Changelog);
            originalVZ.PopisZakazky = SetProperty(originalVZ.PopisZakazky, newVZ.PopisZakazky,
                nameof(originalVZ.PopisZakazky), originalVZ.Changelog);
            originalVZ.RawHtml = SetProperty(originalVZ.RawHtml, newVZ.RawHtml,
                nameof(originalVZ.RawHtml), originalVZ.Changelog);
            originalVZ.KonecnaHodnotaMena = SetProperty(originalVZ.KonecnaHodnotaMena, newVZ.KonecnaHodnotaMena,
                nameof(originalVZ.KonecnaHodnotaMena), originalVZ.Changelog);
            originalVZ.OdhadovanaHodnotaMena = SetProperty(originalVZ.OdhadovanaHodnotaMena, newVZ.OdhadovanaHodnotaMena,
                nameof(originalVZ.OdhadovanaHodnotaMena), originalVZ.Changelog);
            originalVZ.StavVZ = SetProperty(originalVZ.StavVZ, newVZ.StavVZ,
                nameof(originalVZ.StavVZ), originalVZ.Changelog);
            originalVZ.KonecnaHodnotaBezDPH = SetProperty(originalVZ.KonecnaHodnotaBezDPH, newVZ.KonecnaHodnotaBezDPH,
                nameof(originalVZ.KonecnaHodnotaBezDPH), originalVZ.Changelog);
            originalVZ.OdhadovanaHodnotaBezDPH = SetProperty(originalVZ.OdhadovanaHodnotaBezDPH, newVZ.OdhadovanaHodnotaBezDPH,
                nameof(originalVZ.OdhadovanaHodnotaBezDPH), originalVZ.Changelog);
            
            originalVZ.Zadavatel.ProfilZadavatele = SetProperty(originalVZ.Zadavatel.ProfilZadavatele,
                newVZ.Zadavatel.ProfilZadavatele,
                "Zadavatel.ProfilZadavatele",
                originalVZ.Changelog);
            originalVZ.Zadavatel.ICO = SetProperty(originalVZ.Zadavatel.ICO,
                newVZ.Zadavatel.ICO,
                "Zadavatel.ICO",
                originalVZ.Changelog);
            originalVZ.Zadavatel.Jmeno = SetProperty(originalVZ.Zadavatel.Jmeno,
                newVZ.Zadavatel.Jmeno,
                "Zadavatel.Jmeno",
                originalVZ.Changelog);
            
            originalVZ.Zdroje.UnionWith(newVZ.Zdroje);
            originalVZ.CPV.UnionWith(newVZ.CPV);
            originalVZ.UrlZakazky.UnionWith(newVZ.UrlZakazky);
            originalVZ.Formulare.UnionWith(newVZ.Formulare);
        }

        /// <summary>
        /// Sets a property if it should be set. Also if old value is rewritten, then updates changelog.
        /// </summary>
        private static T SetProperty<T>(T oldProp, T newProp, string propName, List<string> changelog)
        {
            //There are no changes at all
            if(newProp is null)
                return oldProp;
            if (newProp is string stringProp && string.IsNullOrWhiteSpace(stringProp))
                return oldProp;
            if (oldProp is not null && oldProp.Equals(newProp))
                return oldProp;
            if (newProp is 0 && oldProp is int and not 0) //default integers shouldnt overwrite value
                return oldProp;
            if (newProp is 0m && oldProp is decimal and not 0m) //default decimals shouldnt overwrite value
                return oldProp;
            if (newProp is DateTime np && np < new DateTime(1900, 1, 1)) //fix for some cases where we do not get correct date
                return oldProp;
  
            changelog?.Add($"{DateTime.Now:yyyy-MM-dd} {propName}:[{oldProp}]=>[{newProp}]");
            
            return newProp;
        }

        private static void SendToOcrQueue(VerejnaZakazka newVZ)
        { 
            if (newVZ.Dokumenty.Any(d => !d.EnoughExtractedText && d.PlainTextContentQuality == DataQualityEnum.Unknown))
            {
                ItemToOcrQueue.AddNewTask(OcrWork.DocTypes.VerejnaZakazka,
                    newVZ.Id,
                    null,
                    DS.Api.OcrWork.TaskPriority.Low);
            }
        }

        /// <summary>
        /// Takes newDoc and fill its missing values from originalDoc.
        /// </summary>
        /// <param name="originalDoc">Document which is going to be updated</param>
        /// <param name="newDoc">Document which may contain additional data</param>
        private static void MergeDocumentProperties(VerejnaZakazka.Document originalDoc, VerejnaZakazka.Document newDoc,
            List<string> changelog)
        {
            int originalChangelogCount = changelog?.Count() ?? 0;
            originalDoc.Name = SetProperty(originalDoc.Name, newDoc.Name, "Document.Name", changelog);
            originalDoc.CisloVerze = SetProperty(originalDoc.CisloVerze, newDoc.CisloVerze, "Document.CisloVerze", changelog);
            originalDoc.ContentType = SetProperty(originalDoc.ContentType, newDoc.ContentType, "Document.ContentType", changelog);
            originalDoc.TypDokumentu = SetProperty(originalDoc.TypDokumentu, newDoc.TypDokumentu, "Document.TypDokumentu", changelog);
            originalDoc.LastProcessed = SetProperty(originalDoc.LastProcessed, newDoc.LastProcessed, "Document.LastProcessed", changelog);
            originalDoc.VlozenoNaProfil = SetProperty(originalDoc.VlozenoNaProfil, newDoc.VlozenoNaProfil, "Document.VlozenoNaProfil", changelog);
            originalDoc.Lenght = SetProperty(originalDoc.Lenght, newDoc.Lenght, "Document.Lenght", changelog);
            originalDoc.Pages = SetProperty(originalDoc.Pages, newDoc.Pages, "Document.Pages", changelog);
            originalDoc.WordCount = SetProperty(originalDoc.WordCount, newDoc.WordCount, "Document.WordCount", changelog);
            
            // text dokumentu do changelogu dávat nechceme, protože by to bylo hodně velké
            originalDoc.PlainText = SetProperty(originalDoc.PlainText, newDoc.PlainText, "Document.PlainText", null);

            originalDoc.DirectUrls.UnionWith(newDoc.DirectUrls);
            originalDoc.OficialUrls.UnionWith(newDoc.OficialUrls);
            originalDoc.StorageIds.UnionWith(newDoc.StorageIds);
            originalDoc.PlainDocumentIds.UnionWith(newDoc.PlainDocumentIds);
            
            
            originalDoc.PlainTextContentQuality = newDoc.PlainTextContentQuality == DataQualityEnum.Unknown
                ? originalDoc.PlainTextContentQuality
                : newDoc.PlainTextContentQuality;

            int currentChangelogCount = changelog?.Count() ?? 0;
            if ( currentChangelogCount > originalChangelogCount)
            {
                originalDoc.LastUpdate = DateTime.Now;
            }
        }

        public static async Task StoreDocumentCopyToHlidacStorageAsync(VerejnaZakazka vz, HttpClient httpClient)
        {
            foreach (var dokument in vz.Dokumenty.Where(d => string.IsNullOrWhiteSpace(d.Sha256Checksum)))
            {
                string fullTempPath = string.Empty;
                string downloadUrl = string.Empty;
                try
                {
                    // download file from web and save it to temporaryPath
                    string temporaryPath = Path.Combine(Config.GetWebConfigValue("VZPrilohyDataPath"),
                        "_temp_");
                    string filename = Guid.NewGuid().ToString("N");
                    Directory.CreateDirectory(temporaryPath);
                    fullTempPath = Path.Combine(temporaryPath, filename);

                    downloadUrl = dokument.GetDocumentUrlToDownload();
                    await DownloadFileAsync(httpClient, downloadUrl, fullTempPath, vz.Id);

                    // calculate checksum first and get length so we can create hlidacStorageId
                    using (var fileStream = File.Open(fullTempPath, FileMode.Open, FileAccess.Read))
                    {
                        dokument.SizeInBytes = fileStream.Length;
                        dokument.Sha256Checksum = await Checksum.DoChecksumAsync(fileStream);
                    }

                    // move file to final destination
                    var hlidacStorageId = dokument.GetHlidacStorageId();
                    var destinationFolder = Init.VzPrilohaLocalCopy.GetFullDir(hlidacStorageId);
                    Directory.CreateDirectory(destinationFolder);
                    var destination = Init.VzPrilohaLocalCopy.GetFullPath(hlidacStorageId);

                    if(!File.Exists(destination))
                        File.Move(fullTempPath, destination);
                }
                catch (Exception e)
                {
                    _logger.Error(e, $"VZ:{vz.Id}, Url:{downloadUrl} - problem during downloading/storing file");
                }
                finally
                {
                    if(!string.IsNullOrWhiteSpace(fullTempPath) && File.Exists(fullTempPath))
                        File.Delete(fullTempPath);
                }
            }
        }

        private static SemaphoreSlim _fileSemaphore = new(1);
        
        private static async Task<HttpResponseMessage> ExecuteHttpRequest(HttpClient httpClient, string url)
        {
            var startTime = DateTime.Now;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            HttpResponseMessage response = null;
            var logResult=String.Empty;
            try
            {
                response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            }
            catch(Exception exception)
            {
                logResult = exception.Message;
                throw;
            }
            finally
            {
                stopWatch.Stop();
                var endTime = startTime.Add(stopWatch.Elapsed);
                if (response is not null)
                {
                    logResult = $"Result {response.StatusCode}";
                }
                //add to bag
                await _fileSemaphore.WaitAsync();
                try
                {
                    await File.AppendAllTextAsync("e:/Data/App/HlidacSmluv/petr/vzres.tsv",
                        $"{startTime:hh:mm:ss.fff}\t{endTime:hh:mm:ss.fff}\t{stopWatch.ElapsedMilliseconds}\t{logResult}\t{url}");

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    _fileSemaphore.Release();
                }
            }
            
            return response;
        }

        private static async Task DownloadFileAsync(HttpClient httpClient,
            string url,
            string path,
            string vzId) 
        {
            // turn off retry
            // using var responseMessage = await _retryPolicy.ExecuteAsync(() =>
            //     httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead));

            var uri = new Uri(url);
            var uriBase = uri.Host;

            var pipeline = _retryThrottledPipelinePerHost.GetOrAdd(uriBase, CreateNewResiliencePipeline());
            
            HttpResponseMessage responseMessage = null;
            try
            {
                responseMessage = await pipeline.ExecuteAsync(async _ =>
                    await ExecuteHttpRequest(httpClient, url));
            }
            catch (RateLimiterRejectedException)
            {
                _logger.Warning($"Rate limit exceeded for Url: {url} for VZ (id:{vzId}).");
                throw;
            }
            catch (Exception exception)
            {
                _logger.Warning($"During downloadFileAsync there was an exception {exception}. For Url: {url} for VZ (id:{vzId}).");
                throw;
            }

            // using var responseMessage = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            
            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.Error($"Url: {url} for VZ (id:{vzId}) returned {responseMessage.StatusCode} status code with reason phrase: {responseMessage.ReasonPhrase}.");
                throw new Exception(
                    $"Can't download file (Url: {url}) for VZ (id:{vzId}). Http status code: {responseMessage.StatusCode}");
            }
            
            try
            {   
                var stream = await responseMessage.Content.ReadAsStreamAsync();
                await using var fileStream = File.Open(path, FileMode.Create);
                
                await stream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
            catch (Exception e)
            {
                _logger.Error(e, $"Url: {url} for VZ (id:{vzId}) can not process stream properly.");
                throw;
            }
        }
        
        private static void SetupUpdateDates(VerejnaZakazka verejnaZakazka, DateTime? posledniZmena, bool updateposledniZmena)
        {
            if (posledniZmena.HasValue)
                verejnaZakazka.PosledniZmena = posledniZmena;
            else if (updateposledniZmena)           
                verejnaZakazka.PosledniZmena = verejnaZakazka.GetPosledniZmena();

            if (updateposledniZmena)
                verejnaZakazka.LastUpdated = DateTime.Now;
        }

        public static async Task<VerejnaZakazka> LoadFromESAsync(string id, ElasticClient client = null)
        {
            client ??= Manager.GetESClient_VerejneZakazky();
            var res = await client.GetAsync<VerejnaZakazka>(id);
            if (res.Found)
                return res.Source;
            else
                return null;
        }
        
        public static async Task<IEnumerable<VerejnaZakazka>> FindDocumentsFromTheSameSourcesAsync(VerejnaZakazka zakazka, ElasticClient elasticClient)
        {
            elasticClient ??= Manager.GetESClient_VerejneZakazky();
            // find possible candidates
            var res = await elasticClient.SearchAsync<VerejnaZakazka>(s => s
                .Query(q => q
                    .Bool(b => b
                        .Must(bm =>
                            bm.Term(t => t.Field(f => f.Zadavatel.ICO).Value(zakazka.Zadavatel.ICO)) &&
                            bm.Terms(t => t.Field("zdroje.uniqueId").Terms(zakazka.Zdroje.Select(z => z.UniqueId)))
                        ))));

            if (!res.IsValid)
            {
                _logger.Warning($"VZ problems with query. {res.DebugInformation}");
                return Enumerable.Empty<VerejnaZakazka>();
            }
            
            if (!res.Hits.Any())
                return Enumerable.Empty<VerejnaZakazka>();
            
            return res.Documents;
        }

        public static async Task<bool> ExistsAsync(string id, ElasticClient client = null)
        {
            client ??= Manager.GetESClient_VerejneZakazky();
            var res = await client.DocumentExistsAsync<VerejnaZakazka>(id);
            return res.Exists;
        }

        public static async Task<string> GetDocumentTextAsync(string documentSha256Checksum, ElasticClient client = null)
        {
            client ??= Manager.GetESClient_VerejneZakazky();
            // find possible candidates
            var res = await client.SearchAsync<VerejnaZakazka>(s => s
                .Query(q => q
                    .Term(t => t.Field("dokumenty.sha256Checksum").Value(documentSha256Checksum))
                ));

            if (!res.IsValid)
            {
                _logger.Warning($"VZ problems with query. {res.DebugInformation}");
                return null;
            }
            
            if (!res.Hits.Any())
                return null;


            var vz = res.Documents.FirstOrDefault();

            var doc = vz?.Dokumenty.FirstOrDefault(d => d.Sha256Checksum == documentSha256Checksum);

            if (doc is not null && doc.EnoughExtractedText)
                return doc.PlainText;

            return null;
        }
    }
}