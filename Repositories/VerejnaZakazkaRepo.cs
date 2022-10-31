using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Util;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
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
            .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(1));
        
        public static async Task UpdatePosledniZmenaAsync(VerejnaZakazka verejnaZakazka, bool force = false, bool save = false)
        {
            DateTime? prevVal = verejnaZakazka.PosledniZmena;
            if (verejnaZakazka.PosledniZmena.HasValue && force == false)
                return;
            else if (verejnaZakazka.LastestFormular() != null)
            {
                verejnaZakazka.PosledniZmena = verejnaZakazka.LastestFormular().Zverejnen;
            }
            else if (verejnaZakazka.DatumUverejneni.HasValue)
            {
                verejnaZakazka.PosledniZmena = verejnaZakazka.DatumUverejneni;
            }
            else
            {
                return;
            }

            if (verejnaZakazka.PosledniZmena != prevVal && save)
                await SaveAsync(verejnaZakazka);
        }

        public static async Task SaveAsync(VerejnaZakazka verejnaZakazka, ElasticClient client = null, DateTime? posledniZmena = null)
        {
            try
            {
                if (string.IsNullOrEmpty(verejnaZakazka.Id))
                    verejnaZakazka.InitId();
                verejnaZakazka.SetStavZakazky();
                verejnaZakazka.LastUpdated = DateTime.Now;
                if (posledniZmena.HasValue)
                    verejnaZakazka.PosledniZmena = posledniZmena;
                else
                    verejnaZakazka.PosledniZmena = verejnaZakazka.GetPosledniZmena();
                var es = client ??await Manager.GetESClient_VZAsync();
                await es.IndexDocumentAsync<VerejnaZakazka>(verejnaZakazka);
            }
            catch (Exception e)
            {
                Consts.Logger.Error(
                    $"VZ ERROR Save ID:{verejnaZakazka.Id} Size:{Newtonsoft.Json.JsonConvert.SerializeObject(verejnaZakazka).Length}", e);
            }
        }
        
        /// <summary>
        /// Update or insert new
        /// </summary>
        /// <param name="newVZ"></param>
        /// <param name="posledniZmena"></param>
        public static async Task UpsertAsync(VerejnaZakazka newVZ, HttpClient httpClient, DateTime? posledniZmena = null)
        {
            if (newVZ is null)
                return;
            
            try
            {
                var elasticClient = await Manager.GetESClient_VZAsync();

                List<Task<VerejnaZakazka>> loadOriginalTasks = new()
                {
                    LoadFromESAsync(newVZ.Id, elasticClient)
                };
                
                string alternativeId =
                    VerejnaZakazka.GenerateId(newVZ.Zadavatel.ProfilZadavatele, newVZ.EvidencniCisloZakazky);
                if (alternativeId != newVZ.Id)
                {
                    loadOriginalTasks.Add(LoadFromESAsync(alternativeId, elasticClient));
                }
                
                var origs = await Task.WhenAll(loadOriginalTasks);
                 
                // If we find more documents, then we do not know original and should throw error
                var originalVZ = origs.Where(r => r != null).SingleOrDefault();

                if (originalVZ is null)
                {
                    SetForOcr(newVZ);
                    SetupUpdateDates(newVZ, posledniZmena);
                    await elasticClient.IndexDocumentAsync(newVZ);
                    return;
                }
                
                //set proper id, because dataset was not the same
                if (originalVZ.Id != newVZ.Id  
                    && newVZ.EvidencniCisloZakazky == originalVZ.EvidencniCisloZakazky) // to make sure both VZ are the same
                {
                    newVZ.Dataset = originalVZ.Dataset;
                    newVZ.InitId(); //fix id

                }
                

                // preferujeme datum z datlabu, protože Rozza nemusí mít nejsprávnější údaj (např VZ P21V00004199)
                newVZ.DatumUverejneni = originalVZ.DatumUverejneni ?? newVZ.DatumUverejneni ;
                
                newVZ.DatumUzavreniSmlouvy ??= originalVZ.DatumUzavreniSmlouvy;
                newVZ.LhutaDoruceni ??= originalVZ.LhutaDoruceni;
                newVZ.LhutaPrihlaseni ??= originalVZ.LhutaPrihlaseni;
                
                newVZ.NazevZakazky = newVZ.NazevZakazky.SetEmptyString(originalVZ.NazevZakazky);
                newVZ.PopisZakazky = newVZ.PopisZakazky.SetEmptyString(originalVZ.PopisZakazky);
                newVZ.RawHtml = newVZ.RawHtml.SetEmptyString(originalVZ.RawHtml);
                newVZ.UrlZakazky = newVZ.UrlZakazky.SetEmptyString(originalVZ.UrlZakazky);
                newVZ.EvidencniCisloZakazky = newVZ.EvidencniCisloZakazky.SetEmptyString(originalVZ.EvidencniCisloZakazky);
                newVZ.KonecnaHodnotaMena = newVZ.KonecnaHodnotaMena.SetEmptyString(originalVZ.KonecnaHodnotaMena);
                newVZ.OdhadovanaHodnotaMena = newVZ.OdhadovanaHodnotaMena.SetEmptyString(originalVZ.OdhadovanaHodnotaMena);
                newVZ.ZakazkaNaProfiluId = newVZ.ZakazkaNaProfiluId.SetEmptyString(originalVZ.ZakazkaNaProfiluId);
                
                newVZ.CPV = newVZ.CPV.Union(originalVZ.CPV).ToArray();
                newVZ.Kriteria = newVZ.Kriteria.Union(originalVZ.Kriteria).ToArray();
                newVZ.Dodavatele = newVZ.Dodavatele.Union(originalVZ.Dodavatele).ToArray();
                newVZ.Formulare = newVZ.Formulare.Union(originalVZ.Formulare).ToArray();
                
                newVZ.Zadavatel.Jmeno = newVZ.Zadavatel.Jmeno.SetEmptyString(originalVZ.Zadavatel.Jmeno);
                newVZ.Zadavatel.ICO = newVZ.Zadavatel.ICO.SetEmptyString(originalVZ.Zadavatel.ICO);
                newVZ.Zadavatel.ProfilZadavatele = newVZ.Zadavatel.ProfilZadavatele.SetEmptyString(originalVZ.Zadavatel.ProfilZadavatele);

                newVZ.StavVZ = (newVZ.StavVZ == (int)VerejnaZakazka.StavyZakazky.Jine) ? originalVZ.StavVZ : newVZ.StavVZ;
                newVZ.KonecnaHodnotaBezDPH ??= originalVZ.KonecnaHodnotaBezDPH;
                newVZ.OdhadovanaHodnotaBezDPH ??= originalVZ.OdhadovanaHodnotaBezDPH;

                //todo: question - jak zjistím, jaký dokument už v db je a jaký není?! (především při updatu)
                await FillDocumentChecksums(newVZ, httpClient);
                await FillDocumentChecksums(originalVZ, httpClient);

                
                var newComparableDocuments = newVZ.Dokumenty.Where(d => d.IsComparable())
                    .ToDictionary(d => d.Sha256Checksum);
                foreach (var origDoc in originalVZ.Dokumenty)
                {
                    //update document by checksum
                    if (origDoc.IsComparable())
                    {
                        if (newComparableDocuments.TryGetValue(origDoc.Sha256Checksum, out var newComparableDoc))
                        {
                            MergeDocuments(newComparableDoc, origDoc);
                            continue;
                        }
                    }

                    // update document by url
                    var newDoc = newVZ.Dokumenty.FirstOrDefault(d =>
                        d.DirectUrl == origDoc.DirectUrl || d.StorageId == origDoc.StorageId);
                    if (newDoc != null)
                    {
                        MergeDocuments(newDoc, origDoc);
                        continue;
                    }
                    
                    // add missing one
                    newVZ.Dokumenty.Add(origDoc);
                }
                
                SetForOcr(newVZ);
                SetupUpdateDates(originalVZ, posledniZmena);
                
                await elasticClient.IndexDocumentAsync<VerejnaZakazka>(newVZ);
            }
            catch (Exception e)
            {
                Consts.Logger.Error(
                    $"VZ ERROR Upserting ID:{newVZ.Id} Size:{Newtonsoft.Json.JsonConvert.SerializeObject(newVZ).Length}", e);
            }
        }

        private static void SetForOcr(VerejnaZakazka newVZ)
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
        /// <param name="newDoc">Document which is going to be updated</param>
        /// <param name="originalDoc">Document which may contain additional data</param>
        private static void MergeDocuments(VerejnaZakazka.Document newDoc, VerejnaZakazka.Document originalDoc)
        {
            if (string.IsNullOrWhiteSpace(newDoc.Sha256Checksum))
            {
                newDoc.SetChecksum(originalDoc.Sha256Checksum);
            }
            
            newDoc.Name = newDoc.Name.SetEmptyString(originalDoc.Name);
            newDoc.CisloVerze = newDoc.CisloVerze.SetEmptyString(originalDoc.CisloVerze);
            newDoc.ContentType = newDoc.ContentType.SetEmptyString(originalDoc.ContentType);
            newDoc.DirectUrl = newDoc.DirectUrl.SetEmptyString(originalDoc.DirectUrl);
            newDoc.OficialUrl = newDoc.OficialUrl.SetEmptyString(originalDoc.OficialUrl);
            newDoc.PlainText = newDoc.PlainText.SetEmptyString(originalDoc.PlainText);
            newDoc.StorageId = newDoc.StorageId.SetEmptyString(originalDoc.StorageId);
            newDoc.TypDokumentu = newDoc.TypDokumentu.SetEmptyString(originalDoc.TypDokumentu);
            newDoc.PlainDocumentId = newDoc.PlainDocumentId.SetEmptyString(originalDoc.PlainDocumentId);

            newDoc.LastProcessed ??= originalDoc.LastProcessed;
            newDoc.LastUpdate ??= originalDoc.LastUpdate;
            newDoc.VlozenoNaProfil ??= originalDoc.VlozenoNaProfil;

            newDoc.Lenght = newDoc.Lenght == 0 ? originalDoc.Lenght : newDoc.Lenght;
            newDoc.Pages = newDoc.Pages == 0 ? originalDoc.Pages : newDoc.Pages;
            newDoc.WordCount = newDoc.WordCount == 0 ? originalDoc.WordCount : newDoc.WordCount;

            newDoc.PlainTextContentQuality = newDoc.PlainTextContentQuality == DataQualityEnum.Unknown
                ? originalDoc.PlainTextContentQuality
                : newDoc.PlainTextContentQuality;
        }

        private static async Task FillDocumentChecksums(VerejnaZakazka vz, HttpClient httpClient)
        {
            foreach (var dokument in vz.Dokumenty.Where(d => string.IsNullOrWhiteSpace(d.Sha256Checksum)))
            {
                string downloadUrl = dokument.GetDocumentUrlToDownload();
                
                var fileContent = await GetFileAsync(httpClient, downloadUrl);
                dokument.SetChecksum(fileContent);
            }
        }

        private static async Task<byte[]> GetFileAsync(HttpClient httpClient, string url)
        {
            using var responseMessage = await _retryPolicy.ExecuteAsync(() => httpClient.GetAsync(url));

            if (!responseMessage.IsSuccessStatusCode)
            {
                Consts.Logger.Warning($"Couldn't get {url}");
                return Array.Empty<byte>();
            }

            // na url není soubor, ale html stránka => nevalidní
            if (responseMessage.Headers.TryGetValues("content-type", out var ct))
            {
                if (ct.Any(t => t.Contains("text/html")))
                {
                    Consts.Logger.Error($"Url: {url} contains only HTML, no file downloaded.");
                    return Array.Empty<byte>();
                }
            }

            return await responseMessage.Content.ReadAsByteArrayAsync();
        }

        public static string SetEmptyString(this string originalValue, string newValue)
        {
            if (string.IsNullOrWhiteSpace(originalValue))
                return newValue;

            return originalValue;
        }

        private static void SetupUpdateDates(VerejnaZakazka newVerejnaZakazka, DateTime? posledniZmena)
        {
            if (posledniZmena.HasValue)
                newVerejnaZakazka.PosledniZmena = posledniZmena;
            else
                newVerejnaZakazka.PosledniZmena = newVerejnaZakazka.GetPosledniZmena();
            newVerejnaZakazka.LastUpdated = DateTime.Now;
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