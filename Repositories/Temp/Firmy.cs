using HlidacStatu.Entities;


using System;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static partial class Firmy
    {

        private static async Task<string> GetNameByIcoAsync(string key)
        {
            var o = await FirmaRepo.FromIcoAsync(key);
            if (o == null)
                return key;
            if (o.Valid == false)
                return key;
            return o.Jmeno;
        }

        private static async Task<Firma> GetByIcoAsync(string key)
        {
            var o = await FirmaRepo.FromIcoAsync(key);
            return o;
        }
        private static async Task<Firma> GetByDSAsync(string key)
        {
            var o = await FirmaRepo.FromDSAsync(key);
            return o ;
        }

        public static Devmasters.Cache.Memcached.Manager<Firma, string> instanceByIco
            = Devmasters.Cache.Memcached.Manager<Firma, string>.GetSafeInstance("firmyByICO_v3_", GetByIcoAsync, TimeSpan.FromHours(4),
                    Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(',')
                   );

        public static Devmasters.Cache.Memcached.Manager<Firma, string> instanceByDS
            = Devmasters.Cache.Memcached.Manager<Firma, string>.GetSafeInstance("firmyByDS_v3_", GetByDSAsync, TimeSpan.FromHours(4),
                    Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(',')
                    );

        public static Devmasters.Cache.Memcached.Manager<string, string> instanceNameOnlyByIco
            = Devmasters.Cache.Memcached.Manager<string, string>.GetSafeInstance("firmaNameOnlyByICO_v3_", GetNameByIcoAsync, TimeSpan.FromHours(12),
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
                return null;

            string normalizedIco = Util.ParseTools.NormalizeIco(ICO);
            if(string.IsNullOrWhiteSpace(normalizedIco))
                return null;
            var f = instanceByIco.Get(normalizedIco);
            if (f == null)
                return null;
            else
                return f;

        }
        public static Firma GetByDS(string ds)
        {
            if (ds == null)
                ds = string.Empty;

            var f = instanceByDS.Get(ds);
            if (f == null)
                return null;
            else
            {
                f.ICO = HlidacStatu.Util.ParseTools.NormalizeIco(f.ICO); //fix
                return f;
            }
        }

    }
}
