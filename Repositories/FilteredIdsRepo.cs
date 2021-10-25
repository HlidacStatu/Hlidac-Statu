using HlidacStatu.Entities;
using HlidacStatu.Util.Cache;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories
{
    public static class FilteredIdsRepo
    {
        public static class CachedIds
        {
            public class QueryBatch
            {
                public string TaskPrefix { get; set; } = string.Empty;

                public string Query { get; set; }
                public Action<string> LogOutputFunc { get; set; } = null;
                public Action<Devmasters.Batch.ActionProgressData> ProgressOutputFunc { get; set; } = null;

            }

            private static volatile ElasticCacheManager<string[], QueryBatch> cacheSmlouvy
                = ElasticCacheManager<string[], QueryBatch>.GetSafeInstance(
                    "cachedIdsSmlouvy",
                    q => smlouvy(q),
                    TimeSpan.FromHours(24),
                    Devmasters.Config.GetWebConfigValue("ESConnection").Split(';'),
                    "DevmastersCache", null, null,
                    key => key.TaskPrefix + Devmasters.Crypto.Hash.ComputeHashToHex(key.Query)
                    );

            public static string[] Smlouvy(QueryBatch query, bool forceUpdate = false)
            {
                if (forceUpdate)
                {
                    cacheSmlouvy.Delete(query);
                }

                return cacheSmlouvy.Get(query);

            }


            private static string[] smlouvy(QueryBatch query)
            {
                if (query == null)
                    return new string[] { };

                if (string.IsNullOrEmpty(query.Query))
                    return new string[] { };

                Func<int, int, Nest.ISearchResponse<Smlouva>> searchFunc = (size, page) =>
                {
                    return ES.Manager.GetESClient().Search<Smlouva>(a => a
                                .Size(size)
                                .Source(false)
                                //.Fields(f => f.Field("Id"))
                                .From(page * size)
                                .Query(q => SmlouvaRepo.Searching.GetSimpleQuery(query.Query))
                                //.Sort(s => s.Ascending(ss => ss.LastUpdate))
                                .Scroll("1m")
                                );
                };

                List<string> ids2Process = new List<string>();
                Repositories.Searching.Tools.DoActionForQuery<Smlouva>(ES.Manager.GetESClient(),
                    searchFunc, (hit, param) =>
                    {
                        ids2Process.Add(hit.Id);
                        return new Devmasters.Batch.ActionOutputData() { CancelRunning = false, Log = null };
                    }, null, query.LogOutputFunc, query.ProgressOutputFunc, false,
                    prefix: "getting ids ");

                return ids2Process.ToArray();
            }
        }

    }
}

