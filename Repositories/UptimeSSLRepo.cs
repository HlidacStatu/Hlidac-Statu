using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static class UptimeSSLRepo
    {

        private static Devmasters.Cache.LocalMemory.AutoUpdatedCache<UptimeSSL[]> uptimeSSlCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedCache<UptimeSSL[]>(TimeSpan.FromHours(2), async (obj) =>
                {
                    UptimeSSL[] res = new UptimeSSL[] { };
                    var resX = await ES.Manager.GetESClient_UptimeSSL()
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

                await Repositories.ES.Manager.GetESClient_UptimeSSL().IndexAsync<UptimeSSL>(item, m => m.Id(item.Id));

            }
            catch (System.Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("UptimeServerRepo.SaveLastCheck error ", e);
                throw;
            }


        }
        public static async Task<UptimeSSL> LoadLatestAsync(string domain)
        {
            var cl = Repositories.ES.Manager.GetESClient_UptimeSSL();

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