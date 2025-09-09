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
    public class DockerSwarmServices : IHealthCheck
    {

        private Options options;

        public class Options : IHCConfig
        {
            public string DefaultSectionName => "Docker.Swarm.Services";
            public string SwarmMonitorUrl { get; set; }
            public Dictionary<string, string> HttpHeaders { get; set; }

            public string[] ExpectedServices { get; set; }
        }

        public DockerSwarmServices(Options options)
        {
            this.options = options;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                bool degraded = false;
                bool bad = false;

                StringBuilder sb = new StringBuilder(1024);

                DockerSwarmHealthCheckResult swarm = (await Devmasters.Net.HttpClient.Simple.GetAsync<ApiResult<DockerSwarmHealthCheckResult>>(
                        options.SwarmMonitorUrl,
                        headers: options.HttpHeaders)
                    )
                    .Data;

                foreach (var svc in this.options.ExpectedServices)
                {
                    if (swarm.Services.Any(m => m.Name.Equals(svc, StringComparison.InvariantCultureIgnoreCase)) == false)
                    {
                        degraded = true;
                        _ = sb.AppendLine($"Service '{svc}' doesn't exists");
                    }
                    else
                    {
                        var service = swarm.Services.First(m => m.Name.Equals(svc, StringComparison.InvariantCultureIgnoreCase));
                        if (service.RunningReplicas == 0)
                        {
                            degraded = true;
                            _ = sb.AppendLine($"Service '{svc}' is stopped");
                        }
                        else if (service.RunningReplicas > 0 && service.RunningReplicas < service.Replicas)
                        {
                            degraded = true;
                            _ = sb.AppendLine($"Service '{svc}' run partly {service.RunningReplicas}/{service.Replicas}");
                        }

                    }
                }

                if (bad)
                    return HealthCheckResult.Unhealthy(sb.ToString());
                else if (degraded)
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


