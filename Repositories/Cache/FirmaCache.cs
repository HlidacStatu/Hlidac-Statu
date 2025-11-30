using System;
using System.Threading.Tasks;
using Hlidacstatu.Caching;
using HlidacStatu.Caching;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using Serilog;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class FirmaCache
{
    // private static readonly ILogger _logger = Log.ForContext(typeof(FirmaCache));

    private static readonly IFusionCache PostgreCache =
        Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2PostgreSql, nameof(FirmaCache));

    private static readonly IFusionCache MemcachedCache =
        Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2Memcache, nameof(FirmaCache));

    public static ValueTask<InfoFact[]> GetInfoFactsAsync(Firma firma) =>
        PostgreCache.GetOrSetAsync($"_InfoFacts:{firma.ICO}",
            _ => FirmaExtension.GetDirectInfoFactsAsync(firma),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))

        );

    public static ValueTask InvalidateInfoFactsAsync(Firma firma) => PostgreCache.ExpireAsync($"_InfoFacts:{firma.ICO}");

    public static ValueTask<Riziko[]> GetRizikoAsync(Firma f, int rok) =>
        PostgreCache.GetOrSetAsync($"_Rizika:{f.ICO}-{rok}",
            _ => FirmaExtension.GetDirectRizikoAsync(f, rok),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static ValueTask InvalidateRizikoAsync(Firma f, int rok) => PostgreCache.ExpireAsync($"_Rizika:{f.ICO}-{rok}");

    public static ValueTask<Firma.Zatrideni.Item[]> GetSubjektyForOborAsync(Firma.Zatrideni.SubjektyObory obor) =>
        MemcachedCache.GetOrSetAsync($"_SubjektyForObor:{obor:G}",
            _ => FirmaRepo.Zatrideni.GetSubjektyDirectAsync(obor),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static FirmaRepo.Merk.MerkEnumConverters.CzechEnumsData GetMerkEnums() =>
        MemcachedCache.GetOrSet($"_MerkEnums",
            _ => FirmaRepo.Merk.MerkEnumConverters.GetMerkEnums(),
                        options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );
}

