using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Lib.Data.External.Camelot
{
    //hloupe reseni
    public class ConnectionPool : IApiConnection
    {

        private class EndpointStatus
        {
            public string Url { get; set; }
            public DateTime Checked { get; set; } = DateTime.MinValue;
            public bool Live { get; set; } = false;
            public DateTime Used { get; set; } = DateTime.MinValue;
        }

        System.Random rnd = new Random(666);
        private System.Collections.Concurrent.ConcurrentDictionary<Guid, EndpointStatus> pool = new System.Collections.Concurrent.ConcurrentDictionary<Guid, EndpointStatus>();
        private System.Timers.Timer timer = new System.Timers.Timer();
        bool insideTimer = false;
        private static object lockObj = new object();
        private static ConnectionPool instance = null;
        private static Devmasters.Logging.Logger logger = new Devmasters.Logging.Logger("Camelot.ConnectionPool");
        private ConnectionPool()
        {
            timer.Interval = 1 * 60 * 1000; //1 min
            timer.Elapsed += (s, e) => CheckAllUris();
            timer.Start();
        }

        private void CheckAllUris()
        {
            if (insideTimer)
                return;

            insideTimer = true;
            DateTime now = DateTime.Now;
            foreach (var id in pool.Keys)
            {
                if (pool.TryGetValue(id, out var status))
                {
                    if (status.Live == false
                        || (now - status.Checked).TotalMinutes < 5
                        || (now - status.Used).TotalMinutes < 5
                        )
                        CheckEndpoint(id, false);
                }

            }

            insideTimer = false;
        }

        public static ConnectionPool DefaultInstance()
        {
            lock (lockObj)
            {
                if (instance == null)
                {
                    instance = new ConnectionPool(Devmasters.Config.GetWebConfigValue("Camelot.Service.Api").Split(";"));
                }
            }
            return instance;
        }

        public ConnectionPool(IEnumerable<Uri> uris)
            : this(uris?.Select(m => m.AbsoluteUri)) { }
        public ConnectionPool(IEnumerable<string> uris)
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
                    logger.Debug($"Added {url} into pool");
                    pool.TryAdd(Guid.NewGuid(), new EndpointStatus() { Url = url });
                }
            }
            if (uris.Count() == 0)
                throw new ArgumentException("No valid API Endpoints");

            CheckAllUris();
        }

        private bool CheckEndpoint(Guid id, bool useIt)
        {
            if (pool.TryGetValue(id, out var status))
            {
                string url = status.Url + "/Camelot/Version";
                try
                {
                    using (Devmasters.Net.HttpClient.URLContent net = new Devmasters.Net.HttpClient.URLContent(url))
                    {
                        logger.Debug($"Testing {url} ");
                        net.Tries = 1;
                        net.Timeout = 2000;
                        var version = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResult<CamelotVersion>>(net.GetContent().Text);
                        DeclareLiveEndpoint(id, useIt);
                        return true;
                    }

                }
                catch (Exception)
                {
                    status.Live = false;
                }
            }
            return false;

        }

        public string GetEndpointUrl()
        {
            var liveUris = pool.Values.Where(m => m.Live).OrderBy(m => m.Used);
            if (liveUris.Count() == 0)
            {
                foreach (var id in pool.Keys)
                {
                    if (pool.TryGetValue(id, out var status))
                    {
                        if (CheckEndpoint(id, true))
                            return status.Url;
                    }
                }
                throw new ApplicationException("No working api enpoint. All are dead");
            }
            var choosen = liveUris.First();
            choosen.Used = DateTime.Now;
            logger.Debug($"Returns {choosen.Url} ");

            return choosen.Url;
        }
        public void DeclareLiveEndpoint(Guid id, bool useIt)
        {
            if (pool.TryGetValue(id, out var item))
            {
                logger.Info($"Url {item.Url} is live.");
                item.Live = true;
                item.Checked = DateTime.Now;
                if (useIt)
                    item.Used = item.Checked;
            }
        }

        public void DeclareDeadEndpoint(string url)
        {
            foreach (var kv in pool.Where(m=>m.Value.Url == url))
            {
                if (pool.TryGetValue(kv.Key, out var item))
                {
                    logger.Warning($"Url {url} is dead.");
                    item.Live = false;
                    item.Checked = DateTime.Now;
                }
            }
        }
    }
}
