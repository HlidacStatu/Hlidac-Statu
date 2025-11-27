using System.Threading.Tasks;
using Hlidacstatu.Caching;
using HlidacStatu.Entities;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories.Statistics;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public class StatisticsCache
{
    private static readonly IFusionCache PermanentCache =
        Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore, nameof(StatisticsCache));
    
    
    public static ValueTask<StatisticsPerYear<Smlouva.Statistics.Data>> GetSmlouvyStatisticsForQueryAsync(string query) =>
        PermanentCache.GetOrSetAsync($"_SmlouvyStatistics:{query}", _ => SmlouvyStatistics.CalculateAsync(query));

    // public static ValueTask InvalidateInfoFactsAsync(Osoba osoba) => PostgreCache.ExpireAsync($"_InfoFacts:{osoba.NameId}");
    
}