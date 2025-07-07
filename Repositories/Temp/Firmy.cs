using HlidacStatu.Entities;


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

        public static Devmasters.Cache.Memcached.Manager<Firma, string> instanceByIco
            = Devmasters.Cache.Memcached.Manager<Firma, string>.GetSafeInstance("firmyByICO_v2_", getByIco, TimeSpan.FromHours(4),
                    Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(',')
                   );

        public static Devmasters.Cache.Memcached.Manager<Firma, string> instanceByDS
            = Devmasters.Cache.Memcached.Manager<Firma, string>.GetSafeInstance("firmyByDS_v2_", getByDS, TimeSpan.FromHours(4),
                    Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(',')
                    );

        public static Devmasters.Cache.Memcached.Manager<string, string> instanceNameOnlyByIco
            = Devmasters.Cache.Memcached.Manager<string, string>.GetSafeInstance("firmaNameOnlyByICO_v2_", getNameByIco, TimeSpan.FromHours(12),
                    Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(',')
                    );

        public static string GetJmeno(int ICO)
        {
            return GetJmeno(ICO.ToString().PadLeft(8, '0'));
        }
        public static string GetJmeno(string ico)
        {
            if (string.IsNullOrWhiteSpace(ico))
                return string.Empty;
            if (Util.DataValidators.CheckCZICO(ico)==false)
                return string.Empty;

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

            string normalizedIco = Util.ParseTools.NormalizeIco(ICO);
            if(string.IsNullOrWhiteSpace(normalizedIco))
                return Firma.LoadError;
            var f = instanceByIco.Get(normalizedIco);
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
            {
                f.ICO = HlidacStatu.Util.ParseTools.NormalizeIco(f.ICO); //fix
                return f;
            }
        }

    }
}
