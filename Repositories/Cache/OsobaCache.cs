using System;
using System.Threading.Tasks;
using Hlidacstatu.Caching;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using Serilog;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class OsobaCache
{
    // private static readonly ILogger _logger = Log.ForContext(typeof(FirmaCache));

    private static readonly IFusionCache PostgreCache =
        Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2PostgreSql, nameof(OsobaCache));

    private static readonly IFusionCache MemcachedCache =
        Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2Memcache, nameof(OsobaCache));

    public static ValueTask<InfoFact[]> GetInfoFactsAsync(Osoba osoba) =>
        PostgreCache.GetOrSetAsync($"_InfoFacts:{osoba.NameId}",
            _ => OsobaExtension.InfoFactsAsync(osoba)
        );

    public static ValueTask InvalidateInfoFactsAsync(Osoba osoba) => PostgreCache.ExpireAsync($"_InfoFacts:{osoba.NameId}");
    
}