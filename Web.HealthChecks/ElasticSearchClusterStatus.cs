using Microsoft.Extensions.Diagnostics.HealthChecks;

using Nest;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class ElasticSearchClusterStatus : IHealthCheck
    {
        private Options options;

        public class Options
        {
            public string[] ElasticServerUris { get; set; }
            public int? ExpectedNumberOfNodes { get; set; } = null;

        }
        public ElasticSearchClusterStatus(Options options)
        {
            this.options = options;
        }

        static ElasticClient _client = null;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {

                //var singlePool = new Elasticsearch.Net.SingleNodeConnectionPool(new Uri(esUrl));
                var pool = new Elasticsearch.Net.StaticConnectionPool(options.ElasticServerUris
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Select(u => new Uri(u))
                    );

                var settings = new ConnectionSettings(pool)
                    .DisableAutomaticProxyDetection(false)
                    .RequestTimeout(TimeSpan.FromSeconds(10))
                    .SniffLifeSpan(null)
                    ;

                if (_client == null)
                    _client = new ElasticClient(settings);

                var res = await _client.Cluster.HealthAsync();

                var numberOfNodes = res?.NumberOfNodes ?? 0;

                string report = $"Num. of nodes:{numberOfNodes}";
                if (options.ExpectedNumberOfNodes.HasValue && options.ExpectedNumberOfNodes.Value != numberOfNodes)
                    report = report + $", {options.ExpectedNumberOfNodes} expected.";
                else
                    report = report + ".";

                switch (res?.Status)
                {
                    case Elasticsearch.Net.Health.Red:
                        return HealthCheckResult.Unhealthy(report + $" Cluster status RED! {res.UnassignedShards} unassigned shards. {res.InitializingShards} initializing shards.");
                    case Elasticsearch.Net.Health.Yellow:
                        return HealthCheckResult.Degraded(report + $" Cluster status YELLOW! {res.UnassignedShards} unassigned shards. {res.InitializingShards} initializing shards.");
                    case Elasticsearch.Net.Health.Green:
                        if (options.ExpectedNumberOfNodes.HasValue && options.ExpectedNumberOfNodes.Value != numberOfNodes)
                            return HealthCheckResult.Degraded(report);
                        else
                            return HealthCheckResult.Healthy(report);
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
