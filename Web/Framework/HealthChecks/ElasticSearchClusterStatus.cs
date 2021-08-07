using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Framework.HealthChecks
{
    public class ElasticSearchClusterStatus : IHealthCheck
    {

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var res = await Repositories.ES.Manager.GetESClient().Cluster.HealthAsync();
                var numberOfNodes = res?.NumberOfNodes ?? 0;

                switch (res?.Status)
                {
                    case Elasticsearch.Net.Health.Red: 
                        return HealthCheckResult.Unhealthy($"Num. of nodes:{numberOfNodes}");
                    case Elasticsearch.Net.Health.Yellow:
                        return HealthCheckResult.Degraded($"Num. of nodes:{numberOfNodes}");
                    case Elasticsearch.Net.Health.Green:
                        return HealthCheckResult.Healthy($"Num. of nodes:{numberOfNodes}");
                    default:
                        return HealthCheckResult.Degraded($"Unknown status");
                }
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }

    }
}
