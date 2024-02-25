using Corsinvest.ProxmoxVE.Api;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class ProxmoxCluster : IHealthCheck
    {

        private Options options;

        public class Options : IHCConfig
        {
            public string DefaultSectionName => "Proxmox.Cluster";
            public string ServerUri { get; set; }
            public string NodeName { get; set; }
            public string ApiToken { get; set; }
            public int ExpectedNumberOfNodes { get; set; }
        }

        public ProxmoxCluster(Options options)
        {
            this.options = options;
        }

        public class ClusterStatus
        {
            public ClusterStatusItem[] Items { get; set; }
            public ClusterStatusItem ClusterInfo { get => Items.FirstOrDefault(m => m.type == "cluster"); }
            public IEnumerable<ClusterStatusItem> Nodes { get => Items.Where(m => m.type == "node"); }
        }

        public class ClusterStatusItem
        {
            public int quorate { get; set; }
            public string type { get; set; }
            public string id { get; set; }
            public int version { get; set; }
            public string name { get; set; }
            public int nodes { get; set; }
            public int nodeid { get; set; }
            public string ip { get; set; }
            public int online { get; set; }
            public string level { get; set; }
            public int local { get; set; }
        }


        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                bool bad = false;
                StringBuilder result = new StringBuilder(1024);
                Uri url = new Uri(this.options.ServerUri);
                var client = new PveClient(url.Host, url.Port != 80 ? url.Port : 8006);
                client.ApiToken = this.options.ApiToken;

                Result resp = await client.Cluster.Status.GetStatus();
                if (resp.ResponseInError == false && resp.IsSuccessStatusCode)
                {
                    ClusterStatus clusterInfo = new ClusterStatus()
                    {
                        Items = Newtonsoft.Json.JsonConvert.DeserializeObject<ClusterStatusItem[]>(JsonConvert.SerializeObject(resp.Response.data))
                    };


                    var onlineNodes = clusterInfo.Nodes.Count(m => m.online == 1);
                    if (onlineNodes < options.ExpectedNumberOfNodes)
                        return HealthCheckResult.Unhealthy($"Number of online nodes {onlineNodes}");
                    else if (onlineNodes != options.ExpectedNumberOfNodes)
                        return HealthCheckResult.Degraded($"Number of online nodes {onlineNodes}");
                    else
                        return HealthCheckResult.Healthy($"Number of online nodes {onlineNodes}");

                }
                else
                    return HealthCheckResult.Unhealthy();


            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy(exception: e);
            }

        }
    }
}
