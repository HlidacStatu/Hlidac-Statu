﻿using HlidacStatu.Entities;


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nest;
using System.Linq;
using HlidacStatu.Connectors;

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

            private static volatile Devmasters.Cache.Redis.Manager<string[], QueryBatch> cacheSmlouvy
                = Devmasters.Cache.Redis.Manager<string[], QueryBatch>.GetSafeInstance(
                    "cachedIdsSmlouvy",
                    q => GetSmlouvyIdAsync(q).ConfigureAwait(false).GetAwaiter().GetResult(),
                    TimeSpan.FromHours(24),
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                    keyValueSelector: key => key.TaskPrefix + Devmasters.Crypto.Hash.ComputeHashToHex(key.Query)
                    );

            public static string[] Smlouvy(QueryBatch query, bool forceUpdate = false)
            {
                if (forceUpdate)
                {
                    cacheSmlouvy.Delete(query);
                }

                return cacheSmlouvy.Get(query);

            }


            private static volatile Devmasters.Cache.Redis.Manager<string[], QueryBatch> cacheDotace
                = Devmasters.Cache.Redis.Manager<string[], QueryBatch>.GetSafeInstance(
                    "cachedIdsDotace",
                    q => GetDotaceIdsAsync(q).ConfigureAwait(false).GetAwaiter().GetResult(),
                    TimeSpan.FromHours(24),
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                    syncTimeoutInMs: 500*1000,
                    keyValueSelector: key => key.TaskPrefix + Devmasters.Crypto.Hash.ComputeHashToHex(key.Query)
                    );

            public static string[] Dotace(QueryBatch query, bool forceUpdate = false)
            {
                if (forceUpdate)
                {
                    cacheDotace.Delete(query);
                }

                return cacheDotace.Get(query);

            }


        }


        public static async Task<string[]> GetDotaceIdsAsync(QueryBatch query, int maxDegreeOfParallelism = 10, Devmasters.Batch.IMonitor monitor = null)
        {
            var client = Manager.GetESClient_Dotace();
            var sq = DotaceRepo.Searching.GetSimpleQuery(query.Query);
            var ids = await Searching.Tools.GetAllIdsAsync(client, maxDegreeOfParallelism, sq,
                logOutputFunc: query.LogOutputFunc, progressOutputFunc: query.ProgressOutputFunc,
                monitor:(monitor ?? new MonitoredTaskRepo.ForBatch(part: $"{nameof(GetDotaceIdsAsync)} {query.Query}") ));

            return ids.Result.ToArray();
        }

        public static async Task<string[]> GetSmlouvyIdAsync(QueryBatch query)
        {
            var client = Manager.GetESClient();

            var ids = await Searching.Tools.GetAllSmlouvyIdsAsync(client, 10, query.Query, 
                logOutputFunc: query.LogOutputFunc, progressOutputFunc: query.ProgressOutputFunc);

            return ids.Result.ToArray();
        }
        public static async Task<string[]> GetSmlouvyIdAsync_old(QueryBatch query)
        {
            var stack = HlidacStatu.Util.StackReport.GetCallingMethod(true);

            if (query == null)
                return new string[] { };

            if (string.IsNullOrEmpty(query.Query))
                return new string[] { };

            Func<int, int, Task<ISearchResponse<Smlouva>>> searchFunc = async (size, page) =>
            {
                var client = Manager.GetESClient();
                return await client.SearchAsync<Smlouva>(a => a
                    .Size(size)
                    .Source(false)
                    .From(page * size)
                    .Query(q => SmlouvaRepo.Searching.GetSimpleQuery(query.Query))
                    .Scroll("1m")
                );
            };

            List<string> ids2Process = new List<string>();
            await Repositories.Searching.Tools.DoActionForQueryAsync<Smlouva>(Manager.GetESClient(),
                searchFunc, (hit, param) =>
                {
                    ids2Process.Add(hit.Id);
                    return new Devmasters.Batch.ActionOutputData() { CancelRunning = false, Log = null };
                }, null, query.LogOutputFunc, query.ProgressOutputFunc, false);

            return ids2Process.ToArray();
        }
    }
}

