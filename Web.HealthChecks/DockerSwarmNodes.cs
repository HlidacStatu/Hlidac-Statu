
using Amazon.Runtime.Internal.Transform;
using HlidacStatu.DS.Api;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class DockerSwarmNodes : IHealthCheck
    {

        private Options options;

        public class Options : IHCConfig
        {
            public string DefaultSectionName => "Docker.Swarm.Nodes";
            public string SwarmMonitorUrl { get; set; }
            public Dictionary<string, string> HttpHeaders { get; set; }

            public int ExpectedNumberOfNodes { get; set; }
            public int ExpectedNumberOfManagers { get; set; }
        }

        public DockerSwarmNodes(Options options)
        {
            this.options = options;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                StringBuilder sb = new StringBuilder(1024);

                DockerSwarmHealthCheckResult swarm = (await Devmasters.Net.HttpClient.Simple.GetAsync<ApiResult<DockerSwarmHealthCheckResult>>(
                        options.SwarmMonitorUrl,
                        headers: options.HttpHeaders)
                    )
                    .Data;

                int activeNodes = swarm.Nodes.Count(m => m.Availability == "active" && m.State == "ready");
                int activeManagers = swarm.Nodes.Count(m => 
                    m.Availability == "active" && m.State == "ready"
                    && (m.Role=="manager" || m.Role == "leader") 
                );

                sb.AppendLine($"active nodes {activeNodes}/{options.ExpectedNumberOfNodes}, ");
                sb.AppendLine($"active managers {activeManagers}/{options.ExpectedNumberOfManagers}, ");
                sb.AppendLine($"active services {swarm.Services.Length}, ");
                sb.AppendLine($"active instance {swarm.Services.Sum(m=>m.RunningReplicas)}.");

                if ( activeNodes <= (options.ExpectedNumberOfNodes*0.7m)) 
                    return HealthCheckResult.Unhealthy(sb.ToString());
                else if (activeNodes != options.ExpectedNumberOfNodes)
                    return HealthCheckResult.Degraded(sb.ToString());
                else
                    return HealthCheckResult.Healthy(sb.ToString());
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy(exception: e);
            }

        }
    }
}


