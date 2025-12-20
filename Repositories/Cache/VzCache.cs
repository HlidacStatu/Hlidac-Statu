using System;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Repositories.Searching;
using Nest;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class VzCache
{
    private static IFusionCache _memoryCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(VzCache));

    public static ValueTask<VerejnaZakazkaSearchData> GetSearchesAsync(string query) =>
        _memoryCache.GetOrSetAsync($"_VzSearch:{query}",
            _ => CachedFuncSimpleSearchAsync(query),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );
    
    public static async Task<VerejnaZakazkaSearchData> CachedSimpleSearchAsync(VerejnaZakazkaSearchData search,
        bool logError = true, bool fixQuery = true, ElasticClient client = null)
    {
        VerejnaZakazkaRepo.Searching.FullSearchQuery q = new VerejnaZakazkaRepo.Searching.FullSearchQuery()
        {
            search = search,
            logError = logError,
            fixQuery = fixQuery,
            client = client
        };
        return await GetSearchesAsync(Newtonsoft.Json.JsonConvert.SerializeObject(q));
    }
    
    private static Task<VerejnaZakazkaSearchData> CachedFuncSimpleSearchAsync(string jsonFullSearchQuery)
    {
        var query = Newtonsoft.Json.JsonConvert.DeserializeObject<VerejnaZakazkaRepo.Searching.FullSearchQuery>(jsonFullSearchQuery);
        return VerejnaZakazkaRepo.Searching.SimpleSearchAsync(query.search, query.anyAggregation, query.logError, query.fixQuery,
            query.client);
    }
    
}

