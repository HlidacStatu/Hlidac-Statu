using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Util;

using Nest;

using System;
using System.Collections.Generic;
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
                var es = client ?? Manager.GetESClient_VZ();
                await es.IndexDocumentAsync<VerejnaZakazka>(verejnaZakazka);
            }
            catch (Exception e)
            {
                Consts.Logger.Error(
                    $"VZ ERROR Save ID:{verejnaZakazka.Id} Size:{Newtonsoft.Json.JsonConvert.SerializeObject(verejnaZakazka).Length}", e);
            }
        }

        public static async Task<VerejnaZakazka> LoadFromESAsync(string id, ElasticClient client = null)
        {
            var es = client ?? Manager.GetESClient_VZ();
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
            var es = client ?? Manager.GetESClient_VZ();
            var res = await es.DocumentExistsAsync<VerejnaZakazka>(id);
            return res.Exists;
        }


    }
}