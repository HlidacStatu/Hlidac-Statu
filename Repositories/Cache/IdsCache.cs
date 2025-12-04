using System;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public class IdsCache
{
    private static readonly IFusionCache PostgreCache =
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2PostgreSql, nameof(FirmaCache));

    public static async Task<string[]> GetSmlouvyIdsAsync(FilteredIdsRepo.QueryBatch query, bool forceUpdate = false)
    {
        string key = $"_cachedIdsSmlouvy:{query.TaskPrefix + Devmasters.Crypto.Hash.ComputeHashToHex(query.Query)}";

        if (forceUpdate)
        {
            await PostgreCache.ExpireAsync(key);
        }

        return await PostgreCache.GetOrSetAsync(key,
            _ => FilteredIdsRepo.GetSmlouvyIdAsync(query),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(8))
        );
    }

    public static async Task<string[]> GetDotaceIdsAsync(FilteredIdsRepo.QueryBatch query, bool forceUpdate = false)
    {
        string key = $"_cachedIdsDotace:{query.TaskPrefix + Devmasters.Crypto.Hash.ComputeHashToHex(query.Query)}";

        if (forceUpdate)
        {
            await PostgreCache.ExpireAsync(key);
        }

        return await PostgreCache.GetOrSetAsync(key,
            _ => FilteredIdsRepo.GetDotaceIdsAsync(query),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(8))
        );
    }
}