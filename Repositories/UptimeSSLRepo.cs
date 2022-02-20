using HlidacStatu.Entities;

using Microsoft.EntityFrameworkCore;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories
{
    public static class UptimeSSLRepo
    {

        private static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeSSL[]> uptimeSSlCache =
            new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<UptimeSSL[]>(TimeSpan.FromHours(12),
                (obj) =>
                {
                    UptimeSSL[] res = new UptimeSSL[] { };
                    var resX = ES.Manager.GetESClient_UptimeSSL()
                        .Search<UptimeSSL>(s => s
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

        public static void Save(UptimeSSL item)
        {
            try
            {

                var res = Repositories.ES.Manager.GetESClient_UptimeSSL().Index<UptimeSSL>(item, m => m.Id(item.Id));

            }
            catch (System.Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error("UptimeServerRepo.SaveLastCheck error ", e);
                throw;
            }


        }
        public static UptimeSSL LoadLatest(string domain)
        {
            var cl = Repositories.ES.Manager.GetESClient_UptimeSSL();

            var res = cl.Search<UptimeSSL>(s => s
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