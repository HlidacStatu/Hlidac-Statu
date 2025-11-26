using System;
using System.Threading.Tasks;
using Hlidacstatu.Caching;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using Serilog;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class FirmaCache
{
    private static readonly ILogger _logger = Log.ForContext(typeof(FirmaCache));

    private static readonly IFusionCache PostgreCache =
        Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2PostgreSql, nameof(FirmaCache));

    private static readonly IFusionCache MemcachedCache =
        Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2Memcache, nameof(FirmaCache));

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

    public static FirmaRepo.Merk.MerkEnumConverters.CzechEnumsData GetMerkEnums()
    {
        var subjekty = MemcachedCache.GetOrSet($"_MerkEnums",
            _ => FirmaRepo.Merk.MerkEnumConverters.GetMerkEnums(),
            options =>
            {
                options.Duration = TimeSpan.FromMinutes(5);
                options.FailSafeMaxDuration = TimeSpan.FromHours(6);
                options.DistributedCacheFailSafeMaxDuration = TimeSpan.FromDays(1);
            } 
        );
        return subjekty;
    }
}