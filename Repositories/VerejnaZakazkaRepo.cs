using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Util;

using Nest;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class VerejnaZakazkaRepo
    {
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
        public static async Task UpsertAsync(VerejnaZakazka newVZ, DateTime? posledniZmena = null)
        {
            if (newVZ is null)
                return;
            
            try
            {
                var elasticClient = await Manager.GetESClient_VZAsync();

                var originalVZ = await LoadFromESAsync(newVZ.Id, elasticClient);

                if (originalVZ is null)
                {
                    //todo: OCR dokumentů here
                    SetupUpdateDates(newVZ, posledniZmena);
                    await elasticClient.IndexDocumentAsync(newVZ);
                    return;
                }

                newVZ.DatumUverejneni ??= originalVZ.DatumUverejneni;
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
                
                newVZ.Kriteria = newVZ.Kriteria.Union(originalVZ.Kriteria).ToArray();
                newVZ.CPV = newVZ.CPV.Union(originalVZ.CPV).ToArray();
                newVZ.Dodavatele = newVZ.Dodavatele.Union(originalVZ.Dodavatele).ToArray();
                newVZ.Formulare = newVZ.Formulare.Union(originalVZ.Formulare).ToArray();
                
                newVZ.Zadavatel.Jmeno = newVZ.Zadavatel.Jmeno.SetEmptyString(originalVZ.Zadavatel.Jmeno);
                newVZ.Zadavatel.ICO = newVZ.Zadavatel.ICO.SetEmptyString(originalVZ.Zadavatel.Jmeno);
                newVZ.Zadavatel.ProfilZadavatele = newVZ.Zadavatel.ProfilZadavatele.SetEmptyString(originalVZ.Zadavatel.ProfilZadavatele);

                newVZ.StavVZ = (newVZ.StavVZ == (int)VerejnaZakazka.StavyZakazky.Jine) ? originalVZ.StavVZ : newVZ.StavVZ;
                newVZ.KonecnaHodnotaBezDPH ??= originalVZ.KonecnaHodnotaBezDPH;
                newVZ.OdhadovanaHodnotaBezDPH ??= originalVZ.OdhadovanaHodnotaBezDPH;

                //todo: question - jak zjistím, jaký dokument už v db je a jaký není?! (především při updatu)
                foreach (var VARIABLE in COLLECTION)
                {
                    
                }
                    
                
                SetupUpdateDates(originalVZ, posledniZmena);
                
                //await es.IndexDocumentAsync<VerejnaZakazka>(newVZ);
            }
            catch (Exception e)
            {
                Consts.Logger.Error(
                    $"VZ ERROR Save ID:{newVZ.Id} Size:{Newtonsoft.Json.JsonConvert.SerializeObject(newVZ).Length}", e);
            }
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