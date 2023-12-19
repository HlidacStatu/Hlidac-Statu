using HlidacStatu.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static class UptimeSSLRepo
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(UptimeSSLRepo));

        private static Devmasters.Cache.LocalMemory.AutoUpdatedCache<UptimeSSL[]> uptimeSSlCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<UptimeSSL[]>(TimeSpan.FromHours(2), (obj) =>
                {
                    UptimeSSL[] res = new UptimeSSL[] { };
                    var client = Manager.GetESClient_UptimeSSLAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                    var resX = client
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
                        ).ConfigureAwait(false).GetAwaiter().GetResult();

                    var latest = ((Nest.BucketAggregate)resX.Aggregations["domains"]).Items
                        .Select(i =>
                        {
                            var valX = ((Nest.KeyedBucket<object>)i).Values.First();
                            UptimeSSL val = ((Nest.TopHitsAggregate)valX).Documents<UptimeSSL>().First();
                            return val;
                        }
                        ).ToArray();

                    return latest;
                });

        public static UptimeSSL[] AllLatestSSL(bool fromCache = true)
        {
            if (fromCache == false)
                uptimeSSlCache.ForceRefreshCache();
            return uptimeSSlCache.Get();
        }

        public static async Task SaveAsync(UptimeSSL item)
        {
            try
            {
                var client = await Manager.GetESClient_UptimeSSLAsync();
                await client.IndexAsync<UptimeSSL>(item, m => m.Id(item.Id));

            }
            catch (System.Exception e)
            {
                _logger.Error(e, "UptimeServerRepo.SaveLastCheck error ");
                throw;
            }


        }
        public static async Task<UptimeSSL> LoadLatestAsync(string domain)
        {
            var cl = await Manager.GetESClient_UptimeSSLAsync();

            var res = await cl.SearchAsync<UptimeSSL>(s => s
                .Query(q=>q
                        .Term(t=>t.Field(f=>f.Domain).Value(domain))
                    )
                .Size(1)
                .Sort(ss=>ss.Descending(d=>d.Created))
            );
            if (res.IsValid)
            {
                if (res.Total > 0)
                    return res.Hits.First().Source;
            }

            return null;
        }


    }
}