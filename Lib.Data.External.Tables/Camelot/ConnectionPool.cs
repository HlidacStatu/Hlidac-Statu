﻿using System;
using System.Collections.Generic;
using System.Linq;
using HlidacStatu.DS.Api;
using Serilog;

namespace HlidacStatu.Lib.Data.External.Tables.Camelot
{
    //hloupe reseni
    public class ConnectionPool : IApiConnection
    {
        private class EndpointStatus
        {
            public string Url { get; set; }
            public DateTime Checked { get; set; } = DateTime.MinValue;
            public bool Ready { get; set; } = false;
            public DateTime Used { get; set; } = DateTime.MinValue;
            public CamelotStatistics Stats = null;
        }

        private System.Collections.Concurrent.ConcurrentDictionary<Guid, EndpointStatus> pool =
            new System.Collections.Concurrent.ConcurrentDictionary<Guid, EndpointStatus>();
        private System.Timers.Timer timer = new System.Timers.Timer();
        bool insideTimer = false;
        private static object lockObj = new object();
        private static ConnectionPool instance = null;

        private readonly ILogger _logger = Log.ForContext<ConnectionPool>();
        
        public readonly string apiKey;

        private ConnectionPool()
        {
            timer.Interval = 20 * 1000; //20s
            timer.Elapsed += (s, e) => CheckAllUris();
        }

        private void CheckAllUris()
        {
            if (insideTimer)
                return;

            insideTimer = true;
            DateTime now = DateTime.Now;

            Devmasters.Batch.Manager.DoActionForAll<Guid>(pool.Keys,
                id =>
                {
                    if (pool.TryGetValue(id, out var status))
                    {
                        CheckEndpoint(id);
                    }

                    return new Devmasters.Batch.ActionOutputData();
                }, null,null,
                true, maxDegreeOfParallelism: pool.Keys.Count, prefix: "camelot CheckAllUris ");
            insideTimer = false;
        }

        public static ConnectionPool DefaultInstance()
        {
            lock (lockObj)
            {
                if (instance == null)
                {
                    instance = new ConnectionPool(Devmasters.Config.GetWebConfigValue("Camelot.Service.Api").Split(";"), Devmasters.Config.GetWebConfigValue("Camelot.Service.Api.Key"));
                }
            }
            return instance;
        }

        public string GetApiKey() => this.apiKey;

        public ConnectionPool(IEnumerable<Uri> uris, string apiKey)
            : this(uris?.Select(m => m.AbsoluteUri), apiKey) { }
        public ConnectionPool(IEnumerable<string> uris, string apiKey)
            : this()
        {
            if (uris == null)
                throw new ArgumentNullException("uris");
            if (uris.Count() == 0)
                throw new ArgumentException("missing API Endpoints");

            foreach (var uri in uris)
            {
                string url = uri;
                if (url.EndsWith("/") || url.EndsWith("\\"))
                    url = url.Substring(0, url.Length - 1);
                if (Uri.TryCreate(url, UriKind.Absolute, out var xxx))
                {
                    _logger.Debug($"Added {url} into pool");
                    pool.TryAdd(Guid.NewGuid(), new EndpointStatus() { Url = url });
                }
            }
            if (uris.Count() == 0)
                throw new ArgumentException("No valid API Endpoints");

            this.apiKey = apiKey;

            CheckAllUris();
            timer.Start();
        }

        private bool CheckEndpoint(Guid id)
        {
            if (pool.TryGetValue(id, out var status))
            {
                string url = status.Url + "/Camelot/Statistic";
                try
                {
                    using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent(url))
                    {
                        net.RequestParams.Headers.Add("Authorization", apiKey);
                        _logger.Debug($"Testing {url} ");
                        net.Tries = 1;
                        net.Timeout = 2000;
                        var stat = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<CamelotStatistics>>(net.GetContent().Text);
                        status.Stats = stat.Data;
                        if (stat.Success && stat.Data.CurrentThreads <= stat.Data.MaxThreads)
                        {
                            DeclareLiveEndpoint(id);
                            return true;

                        }
                        else
                        {
                            DeclareDeadEndpoint(url);
                            return true;
                        }
                    }

                }
                catch (Exception)
                {

                    DeclareDeadEndpoint(url);
                }
            }
            return false;

        }

        public string GetEndpointUrl()
        {
            var liveUris = pool
                .Where(m => m.Value.Ready)
                .OrderBy(m => m.Value.Stats?.UsedThreadsPercent() ?? 0).ThenBy(m => m.Value.Used);

            if (liveUris.Count() == 0)
            {
                foreach (var id in pool.Keys)
                {
                    if (pool.TryGetValue(id, out var status))
                    {
                        if (CheckEndpoint(id))
                        {
                            UseLiveEndpoint(id);
                            return status.Url;
                        }
                    }
                }
                throw new ApplicationException("No working api enpoint. All are dead");
            }
            else
                _logger.Debug("GetEndpointUrl list:"
                    + string.Join(";",liveUris.Select(m =>$"{m.Value.Url} thr:{m.Value.Stats.UsedThreadsPercent():P1}"))
                    );

            var choosen = liveUris.First();
            UseLiveEndpoint(choosen.Key);
            return choosen.Value.Url;
        }
        public void UseLiveEndpoint(Guid id)
        {
            if (pool.TryGetValue(id, out var item))
            {
                _logger.Information($"Choosen {item.Url} used threads {item.Stats.UsedThreadsPercent():P1}.");
                item.Used = DateTime.Now;
            }
        }
        public void DeclareLiveEndpoint(Guid id)
        {
            if (pool.TryGetValue(id, out var item))
            {
                _logger.Information($"Url {item.Url} is live.  used threads {item.Stats.UsedThreadsPercent():P1}");
                item.Ready = true;
                item.Checked = DateTime.Now;
            }
        }

        public void DeclareDeadEndpoint(string url)
        {
            foreach (var kv in pool.Where(m => m.Value.Url == url))
            {
                if (pool.TryGetValue(kv.Key, out var item))
                {
                    _logger.Warning($"Url {url} is dead.");
                    item.Ready = false;
                    item.Checked = DateTime.Now;
                }
            }
        }
    }
}
