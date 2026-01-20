using HlidacStatu.Entities;


using System;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories
{
    public static partial class Firmy
    {
        private static IFusionCache MemcachedCache =>
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2Memcache, nameof(FirmaCache));
        

        public static async Task<string> GetJmenoAsync(int ICO)
        {
            return await GetJmenoAsync(ICO.ToString().PadLeft(8, '0'));
        }
        public static async Task<string> GetJmenoAsync(string ico)
        {
            if (string.IsNullOrWhiteSpace(ico))
                return string.Empty;
            if (Util.DataValidators.CheckCZICO(ico)==false)
                return string.Empty;
            var icoNormalized = Util.ParseTools.NormalizeIco(ico); 

            return await MemcachedCache.GetOrSetAsync($"_firmaNameOnlyByICO_v4:{icoNormalized}",
                async _ =>
                {
                    var o = await FirmaRepo.FromIcoAsync(icoNormalized);
                    if (o == null)
                        return icoNormalized;
                    if (o.Valid == false)
                        return icoNormalized;
                    return o.Jmeno;
                },
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12))
            );
        }
        public static async Task<Firma> GetAsync(int ICO)
        {
            return await GetAsync(ICO.ToString().PadLeft(8, '0'));
        }
        public static async Task<Firma> GetAsync(string ICO)
        {
            if (string.IsNullOrEmpty(ICO))
                return null;

            string normalizedIco = Util.ParseTools.NormalizeIco(ICO);
            if(string.IsNullOrWhiteSpace(normalizedIco))
                return null;
            
            return await MemcachedCache.GetOrSetAsync($"_firmyByICO_v4:{normalizedIco}",
                async _ => await FirmaRepo.FromIcoAsync(normalizedIco),
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(4))
            );

        }
        public static async Task<Firma> GetByDSAsync(string ds)
        {
            if (ds == null)
                ds = string.Empty;

            return await MemcachedCache.GetOrSetAsync($"_firmyByDS_v4:{ds}",
                async _ => await FirmaRepo.FromDSAsync(ds),
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(4))
            );
        }

    }
}
