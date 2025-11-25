using System.Threading.Tasks;
using Caching;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class FirmaCache
{
    private static readonly IFusionCache PostgreCache =
        Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2PostgreSql, nameof(FirmaCache));

    private static readonly IFusionCache MemcachedCache =
        Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2Memcache, nameof(FirmaCache));

    public static async Task<InfoFact[]> GetInfoFactsAsync(Firma firma)
    {
        var infoFacts = await PostgreCache.GetOrSetAsync($"_InfoFacts:{firma.ICO}",
            _ => FirmaExtension.GetDirectInfoFactsAsync(firma)
        );
        return infoFacts;
    }
    
    public static async Task InvalidateInfoFactsAsync(Firma firma)
    {
        await PostgreCache.ExpireAsync($"_InfoFacts:{firma.ICO}");
    }

    public static async Task<Riziko[]> GetRizikoAsync(Firma f, int rok)
    {
        var rizika = await PostgreCache.GetOrSetAsync($"_Rizika:{f.ICO}-{rok}",
            _ => FirmaExtension.GetDirectRizikoAsync(f, rok)
        );
        return rizika;
    }
    
    public static async Task InvalidateRizikoAsync(Firma f, int rok)
    {
        await PostgreCache.ExpireAsync($"_Rizika:{f.ICO}-{rok}");
    }
    
    public static async Task<Firma.Zatrideni.Item[]> GetSubjektyForOborAsync(Firma.Zatrideni.SubjektyObory obor)
    {
        var subjekty = await MemcachedCache.GetOrSetAsync($"_SubjektyForObor:{obor:G}",
            _ => FirmaRepo.Zatrideni.GetSubjektyDirectAsync(obor)
        );
        return subjekty;
    }
    
    
}