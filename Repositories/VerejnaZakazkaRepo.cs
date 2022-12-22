using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Util;

using Nest;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Devmasters.Collections;
using HlidacStatu.Entities;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;


namespace HlidacStatu.Repositories
{
    public static partial class VerejnaZakazkaRepo
    {
        private static AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.RequestMessage.RequestUri.Host.Contains("bpapi.datlab.eu") &&
                           r.StatusCode == HttpStatusCode.Forbidden)
            .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(1));

        // Emergency HttpClient for cases where it is not convinient to inject HttpClient.
        private static Lazy<HttpClient> _lazyHttpClient = new();
        
        /// <summary>
        /// Update or insert new
        /// </summary>
        /// <param name="newVZ"></param>
        /// <param name="posledniZmena"></param>
        public static async Task UpsertAsync(VerejnaZakazka newVZ, HttpClient httpClient = null, DateTime? posledniZmena = null)
        {
            if (newVZ is null)
                return;

            if (httpClient is null)
                httpClient = _lazyHttpClient.Value;
            
            try
            {
                //todo: změnit na:
                //1) stáhnout soubor
                //2) spočítat jeho checksum a velikost
                //3) uložit soubor do Hlídač úložiště
                //4) !pozor - je potřeba to celé zabalit do FillDocumentChecksums a přejmenovat
                
                var newDocChecksumTask = FillDocumentChecksums(newVZ, httpClient);
                SetupUpdateDates(newVZ, posledniZmena);
                
                var elasticClient = await Manager.GetESClient_VZAsync();

                var originalVZ = await FindOriginalDocumentFromESAsync(newVZ);

                await newDocChecksumTask;
                // Merge possible document duplicates
                MergeDuplicateDocuments(newVZ);


                // VZ neexistuje => ukládáme
                if (originalVZ is null)
                {
                    SendToOcrQueue(newVZ);
                    await elasticClient.IndexDocumentAsync(newVZ);
                    return;
                }
                
                //VZ existuje => mergujeme
                // zajistit, že budeme mít checksum všude
                var origDocChecksumTask = FillDocumentChecksums(originalVZ, httpClient);
                
                MergeSimpleProperties(ref originalVZ, newVZ);

                // merge dodavatele
                foreach (var newDodavatel in newVZ.Dodavatele)
                {
                    var origDodavatel = originalVZ.Dodavatele.Where(d => d.Equals(newDodavatel)).FirstOrDefault();

                    if (origDodavatel is not null)
                    {
                        origDodavatel.Jmeno = SetProperty(origDodavatel.Jmeno, newDodavatel.Jmeno, "Dodavatel.Jmeno", originalVZ.Changelog);
                    }
                    else
                    {
                        originalVZ.Dodavatele.Add(newDodavatel);
                    }
                }    
                
                //merge dokumenty
                //todo: potřebuju zrevidovat OCR, aby se používal stejný update, jinak se to celé rozbije
                await origDocChecksumTask;
                foreach (var newDoc in newVZ.Dokumenty)
                {

                    var origDoc = originalVZ.Dokumenty.FirstOrDefault(d => d.Equals(newDoc));
                    // update document by checksum
                    if (origDoc is not null)
                    {
                        MergeDocuments(origDoc, newDoc, originalVZ.Changelog);
                        continue;
                    }

                    // add missing one
                    originalVZ.Dokumenty.Add(newDoc);
                }
                
                SendToOcrQueue(originalVZ);
                
                await elasticClient.IndexDocumentAsync<VerejnaZakazka>(originalVZ);
            }
            catch (Exception e)
            {
                Consts.Logger.Error(
                    $"VZ ERROR Upserting ID:{newVZ.Id} Size:{Newtonsoft.Json.JsonConvert.SerializeObject(newVZ).Length}", e);
            }
        }

        private static void MergeDuplicateDocuments(VerejnaZakazka newVZ)
        {
            var groups = newVZ.Dokumenty
                .GroupBy(d => d.Sha256Checksum)
                .Where(g => g.Count() > 1);
            foreach (var group in groups)
            {
                var main = group.FirstOrDefault();
                foreach (var duplicate in group.Skip(1))
                {
                    MergeDocuments(main, duplicate, newVZ.Changelog);
                }
            }
        }

        public static async Task UpdateDocumentsInVz(string Id, List<VerejnaZakazka.Document> dokumenty)
        {
            
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
            

            originalVZ.CPV.UnionWith(newVZ.CPV);
            originalVZ.UrlZakazky.UnionWith(newVZ.UrlZakazky);
            originalVZ.Formulare.UnionWith(newVZ.Formulare);
            originalVZ.Kriteria.UnionWith(newVZ.Kriteria);
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
            if (newProp is int and 0 && oldProp is int and not 0) //default integers shouldnt overwrite value
                return oldProp;
            if (newProp is decimal and 0m && oldProp is decimal and not 0m) //default decimals shouldnt overwrite value
                return oldProp;
  
            changelog?.Add($"{DateTime.Now:yyyy-MM-dd} {propName}:[{oldProp}]=>[{newProp}]");
            
            return newProp;
        }

        private static void SendToOcrQueue(VerejnaZakazka newVZ)
        {
            if (newVZ.Dokumenty.Any(d => !d.EnoughExtractedText))
            {
                ItemToOcrQueue.AddNewTask(ItemToOcrQueue.ItemToOcrType.VerejnaZakazka,
                    newVZ.Id,
                    null,
                    HlidacStatu.Lib.OCR.Api.Client.TaskPriority.Low);
            }
        }

        /// <summary>
        /// Takes newDoc and fill its missing values from originalDoc.
        /// </summary>
        /// <param name="originalDoc">Document which is going to be updated</param>
        /// <param name="newDoc">Document which may contain additional data</param>
        private static void MergeDocuments(VerejnaZakazka.Document originalDoc, VerejnaZakazka.Document newDoc,
            List<string> changelog)
        {
            originalDoc.Name = SetProperty(originalDoc.Name, newDoc.Name, "Document.Name", changelog);
            originalDoc.CisloVerze = SetProperty(originalDoc.CisloVerze, newDoc.CisloVerze, "Document.CisloVerze", changelog);
            originalDoc.ContentType = SetProperty(originalDoc.ContentType, newDoc.ContentType, "Document.ContentType", changelog);
            originalDoc.TypDokumentu = SetProperty(originalDoc.TypDokumentu, newDoc.TypDokumentu, "Document.TypDokumentu", changelog);
            originalDoc.LastProcessed = SetProperty(originalDoc.LastProcessed, newDoc.LastProcessed, "Document.LastProcessed", changelog);
            originalDoc.LastUpdate = SetProperty(originalDoc.LastUpdate, newDoc.LastUpdate, "Document.LastUpdate", changelog);
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
        }

        public static async Task FillDocumentChecksums(VerejnaZakazka vz, HttpClient httpClient)
        {
            foreach (var dokument in vz.Dokumenty.Where(d => string.IsNullOrWhiteSpace(d.Sha256Checksum)))
            {
                string downloadUrl = dokument.GetDocumentUrlToDownload();

                string checksum;
                try
                {
                    checksum = await ProcessHttpFileStreamAsync(httpClient, downloadUrl, Checksum.DoChecksumAsync);
                }
                catch (Exception e)
                {
                    checksum = Checksum.GenerateInvalidChecksum();
                }
                
                dokument.Sha256Checksum = checksum;
            }
        }

        private static async Task<T> ProcessHttpFileStreamAsync<T>(HttpClient httpClient,
            string url,
            Func<Stream, Task<T>> asyncProcessingMethod) 
        {
            if (asyncProcessingMethod == null) throw new ArgumentNullException(nameof(asyncProcessingMethod));

            using var responseMessage = await _retryPolicy.ExecuteAsync(() => httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead));

            if (!responseMessage.IsSuccessStatusCode)
            {
                Consts.Logger.Warning($"Couldn't get {url}");
                return default;
            }

            try
            {
                var stream = await responseMessage.Content.ReadAsStreamAsync();
                var result = await asyncProcessingMethod(stream);
                return result;
            }
            catch (Exception e)
            {
                Consts.Logger.Error($"Url: {url} can not process stream properly.", e);
                throw;
            }
        }
        
        private static void SetupUpdateDates(VerejnaZakazka verejnaZakazka, DateTime? posledniZmena)
        {
            if (posledniZmena.HasValue)
                verejnaZakazka.PosledniZmena = posledniZmena;
            else
                verejnaZakazka.PosledniZmena = verejnaZakazka.GetPosledniZmena();
            verejnaZakazka.LastUpdated = DateTime.Now;

            foreach (var zdroj in verejnaZakazka.Zdroje)
            {
                zdroj.Modified = DateTime.Now;
            }
            
            foreach (var dokument in verejnaZakazka.Dokumenty)
            {
                dokument.LastUpdate = DateTime.Now;
            }
        }

        public static async Task<VerejnaZakazka> LoadFromESAsync(string id, ElasticClient client = null)
        {
            var es = client ??await Manager.GetESClient_VZAsync();
            var res = await es.GetAsync<VerejnaZakazka>(id);
            if (res.Found)
                return res.Source;
            else
                return null;
        }
        
        //todo: test it!
        public static async Task<VerejnaZakazka> FindOriginalDocumentFromESAsync(VerejnaZakazka zakazka)
        {
            var es = await Manager.GetESClient_VZAsync();
            // find possible candidates
            var res = await es.SearchAsync<VerejnaZakazka>(s => s
                .Query(q => q
                    .Bool(b => b
                        .Must(bm =>
                            bm.Term(t => t.Field(f => f.Zadavatel.ICO).Value(zakazka.Zadavatel.ICO)) &&
                            bm.Terms(t => t.Field("zdroje.uniqueId").Terms(zakazka.Zdroje.Select(z => z.UniqueId)))
                        ))));

            if (!res.IsValid)
            {
                Consts.Logger.Warning($"VZ problems with query. {res.DebugInformation}");
                return null;
            }
            
            if (res.Hits.Count() <= 0)
                return null;

            if (res.Hits.Count > 1)
            {
                Consts.Logger.Warning($"VZ Too many matches found. Only one is allowed. \n" +
                                      $"Ico: {zakazka.Zadavatel.ICO}\n" +
                                      $"Zdroje:{zakazka.VypisZdroju()}");
                return null;
            }

            
            return res.Documents.SingleOrDefault();
        }

        public static Task<bool> ExistsAsync(VerejnaZakazka vz, ElasticClient client = null)
        {
            return ExistsAsync(vz.Id, client);
        }

        public static async Task<bool> ExistsAsync(string id, ElasticClient client = null)
        {
            var es = client ?? await Manager.GetESClient_VZAsync();
            var res = await es.DocumentExistsAsync<VerejnaZakazka>(id);
            return res.Exists;
        }


    }
}