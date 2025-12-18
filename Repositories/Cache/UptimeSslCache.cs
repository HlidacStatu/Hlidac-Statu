using System;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class UptimeSslCache
{
    private static IFusionCache _memoryCache =>
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(UptimeSslCache));

        public static ValueTask<UptimeSSL[]> GetUptimeSslCacheAsync() =>
            _memoryCache.GetOrSetAsync($"_UptimeSslCache", async _ => {
                    UptimeSSL[] res = new UptimeSSL[] { };
                    var client = Manager.GetESClient_UptimeSSL();
                    var resX = await client
                        .SearchAsync<UptimeSSL>(s => s
                            .Query(q => q.MatchAll())
                            .Aggregations(agg => agg
                                .Terms("domains", t => t
                                    .Field(ff => ff.Domain)
                                    .Size(5000)
                                    .Aggregations(a => a
                                        .TopHits("last", th => th
                                            .Size(1)
                                            .Sort(o => o.Descending("created"))
                                        )
                                    )
                                )
                            )
                        );

                    var latest = ((Nest.BucketAggregate)resX.Aggregations["domains"]).Items
                        .Select(i =>
                            {
                                var valX = ((Nest.KeyedBucket<object>)i).Values.First();
                                UptimeSSL val = ((Nest.TopHitsAggregate)valX).Documents<UptimeSSL>().First();
                                return val;
                            }
                        ).ToArray();

                    return latest;
                },
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(2))
            );
        
        public static ValueTask InvalidateUptimeSslCacheAsync() => _memoryCache.ExpireAsync($"_UptimeSslCache");

        public static async Task<UptimeSSL[]> AllLatestSslAsync(bool fromCache = true)
        {
            if (fromCache == false)
                await InvalidateUptimeSslCacheAsync();
            return await GetUptimeSslCacheAsync();
        }
    
}

