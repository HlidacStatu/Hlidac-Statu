using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Util;

using Nest;

using System;
using System.Collections.Generic;

namespace HlidacStatu.Repositories
{
    public static partial class VerejnaZakazkaRepo
    {
        public static void UpdatePosledniZmena(VerejnaZakazka verejnaZakazka, bool force = false, bool save = false)
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
                Save(verejnaZakazka);
        }

        public static void Save(VerejnaZakazka verejnaZakazka, ElasticClient client = null, DateTime? posledniZmena = null)
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
                es.IndexDocument<VerejnaZakazka>(verejnaZakazka);
            }
            catch (Exception e)
            {
                Consts.Logger.Error(
                    $"VZ ERROR Save ID:{verejnaZakazka.Id} Size:{Newtonsoft.Json.JsonConvert.SerializeObject(verejnaZakazka).Length}", e);
            }
        }

        public static VerejnaZakazka LoadFromES(string id, ElasticClient client = null)
        {
            var es = client ?? Manager.GetESClient_VZ();
            var res = es.Get<VerejnaZakazka>(id);
            if (res.Found)
                return res.Source;
            else
                return null;
        }

        public static bool Exists(VerejnaZakazka vz, ElasticClient client = null)
        {
            return Exists(vz.Id, client);
        }

        public static bool Exists(string id, ElasticClient client = null)
        {
            var es = client ?? Manager.GetESClient_VZ();
            var res = es.DocumentExists<VerejnaZakazka>(id);
            return res.Exists;
        }


    }
}