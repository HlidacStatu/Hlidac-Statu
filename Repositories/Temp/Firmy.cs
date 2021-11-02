using HlidacStatu.Entities;
using HlidacStatu.Util.Cache;

using System;

namespace HlidacStatu.Repositories
{
    public static partial class Firmy
    {

        static Firma nullObj = new Firma();


        private static string getNameByIco(string key)
        {
            var o = FirmaRepo.FromIco(key);
            if (o == null)
                return key;
            if (o.Valid == false)
                return key;
            return o.Jmeno;
        }

        private static Firma getByIco(string key)
        {
            var o = FirmaRepo.FromIco(key);
            return o ?? nullObj;
        }
        private static Firma getByDS(string key)
        {
            var o = FirmaRepo.FromDS(key);
            return o ?? nullObj;
        }

        public static CouchbaseCacheManager<Firma, string> instanceByIco
            = CouchbaseCacheManager<Firma, string>.GetSafeInstance("firmyByICO_v2_", getByIco, TimeSpan.FromHours(4),
                Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                Devmasters.Config.GetWebConfigValue("CouchbasePassword"));

        public static CouchbaseCacheManager<Firma, string> instanceByDS
            = CouchbaseCacheManager<Firma, string>.GetSafeInstance("firmyByDS_v2_", getByDS, TimeSpan.FromHours(4),
                Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                Devmasters.Config.GetWebConfigValue("CouchbasePassword"));

        public static CouchbaseCacheManager<string, string> instanceNameOnlyByIco
            = CouchbaseCacheManager<string, string>.GetSafeInstance("firmaNameOnlyByICO_v2_", getNameByIco, TimeSpan.FromHours(12),
                Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                Devmasters.Config.GetWebConfigValue("CouchbasePassword"));

        public static string GetJmeno(int ICO)
        {
            return GetJmeno(ICO.ToString().PadLeft(8, '0'));
        }
        public static string GetJmeno(string ico)
        {
            return instanceNameOnlyByIco.Get(Util.ParseTools.NormalizeIco(ico));
        }
        public static Firma Get(int ICO)
        {
            return Get(ICO.ToString().PadLeft(8, '0'));
        }
        public static Firma Get(string ICO)
        {
            if (string.IsNullOrEmpty(ICO))
                return Firma.LoadError;
            var f = instanceByIco.Get(Util.ParseTools.NormalizeIco(ICO));
            if (f == null)
                return Firma.LoadError;
            else
                return f;

        }
        public static Firma GetByDS(string ds)
        {
            if (ds == null)
                ds = string.Empty;

            var f = instanceByDS.Get(ds);
            if (f == null)
                return Firma.LoadError;
            else
                return f;

        }

    }
}
