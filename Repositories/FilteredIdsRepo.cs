using HlidacStatu.Entities;
using HlidacStatu.Util.Cache;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nest;

namespace HlidacStatu.Repositories
{
    public static class FilteredIdsRepo
    {
        public class QueryBatch
        {
            public string TaskPrefix { get; set; } = string.Empty;

            public string Query { get; set; }
            public Action<string> LogOutputFunc { get; set; } = null;
            public Action<Devmasters.Batch.ActionProgressData> ProgressOutputFunc { get; set; } = null;

        }
        
        public static class CachedIds
        {

            private static volatile ElasticCacheManager<string[], QueryBatch> cacheSmlouvy
                = ElasticCacheManager<string[], QueryBatch>.GetSafeInstance(
                    "cachedIdsSmlouvy",
                    q => GetSmlouvy(q),
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


            
        }

        public static string[] GetSmlouvy(QueryBatch query)
        {
            var stack = HlidacStatu.Util.StackReport.GetCallingMethod(true);

            if (query == null)
                return new string[] { };

            if (string.IsNullOrEmpty(query.Query))
                return new string[] { };

            Func<int, int, Task<ISearchResponse<Smlouva>>> searchFunc = (size, page) =>
            {
                return ES.Manager.GetESClient().SearchAsync<Smlouva>(a => a
                    .Size(size)
                    .Source(false)
                    .From(page * size)
                    .Query(q => SmlouvaRepo.Searching.GetSimpleQuery(query.Query))
                    .Scroll("1m")
                );
            };

            List<string> ids2Process = new List<string>();
            Repositories.Searching.Tools.DoActionForQuery<Smlouva>(ES.Manager.GetESClient(),
                searchFunc, (hit, param) =>
                {
                    ids2Process.Add(hit.Id);
                    return new Devmasters.Batch.ActionOutputData() { CancelRunning = false, Log = null };
                }, null, query.LogOutputFunc, query.ProgressOutputFunc, false);

            return ids2Process.ToArray();
        }
    }
}

