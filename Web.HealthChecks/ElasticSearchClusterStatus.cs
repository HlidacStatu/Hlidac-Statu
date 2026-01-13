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
            public string[] ExpectedAddresses { get; set; } = null;

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

                string report = $"Num. of runningNodes:{numberOfNodes}";
                if (options.ExpectedNumberOfNodes.HasValue && options.ExpectedNumberOfNodes.Value != numberOfNodes)
                {
                    report = report + $", {options.ExpectedNumberOfNodes} expected.";
                    if (options.ExpectedAddresses?.Length > 0)
                    {
                        //GET /_cat/runningNodes?v&h=m,name,ip,u&s=name
                        var catr = (await _client.LowLevel.Cat.NodesAsync<Elasticsearch.Net.StringResponse>(
                                new Elasticsearch.Net.Specification.CatApi.CatNodesRequestParameters()
                                {
                                    Headers = new[] { "ip" },
                                    Verbose = true
                                }
                            )).Body;
                        string[] runningNodes = catr.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        string missingExpected = string.Join(", ", options.ExpectedAddresses.Except(runningNodes)).Trim();
                        string notExpected = string.Join(", ", runningNodes.Except(options.ExpectedAddresses)).Trim();

                        if (!string.IsNullOrEmpty(missingExpected))
                            report = report + " Neběžící servery:" + missingExpected + ".";
                        if (!string.IsNullOrEmpty(notExpected))
                            report = report + " Nečekané servery:" + notExpected + ".";
                    }
                }
                else
                    report = report + ".";



                if (res?.Status == Elasticsearch.Net.Health.Red)
                    return HealthCheckResult.Unhealthy(report + $" Cluster status RED! {res.UnassignedShards} unassigned shards. {res.InitializingShards} initializing shards.");
                else if (res?.Status == Elasticsearch.Net.Health.Yellow)
                    return HealthCheckResult.Degraded(report + $" Cluster status YELLOW! {res.UnassignedShards} unassigned shards. {res.InitializingShards} initializing shards.");
                else if (options.ExpectedNumberOfNodes.HasValue && options.ExpectedNumberOfNodes.Value != numberOfNodes)
                    return HealthCheckResult.Degraded(report);

                else if (res?.Status == Elasticsearch.Net.Health.Green)
                    return HealthCheckResult.Healthy(report);
                else
                    return HealthCheckResult.Degraded($"Unknown Elastic status");

            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }

    }
}
