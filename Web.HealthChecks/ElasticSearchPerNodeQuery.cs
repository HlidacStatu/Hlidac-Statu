using Microsoft.Extensions.Diagnostics.HealthChecks;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class ElasticSearchPerNodeQuery : IHealthCheck
    {
        private Options options;

        public class Options
        {
            public string[] ElasticClusterUris { get; set; }
            public string Query { get; set; }
            public string IndexName { get; set; }
            public TimeSpan WarningResponseTime { get; set; } = TimeSpan.FromMilliseconds(500);
            public TimeSpan ErrorResponseTime { get; set; } = TimeSpan.FromMilliseconds(1500);
        }
        public ElasticSearchPerNodeQuery(Options options)
        {
            this.options = options;
        }


        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                //get list of nodes
                var poolA = new Elasticsearch.Net.StaticConnectionPool(options.ElasticClusterUris
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Select(u => new Uri(u))
                    );

                var settingsA = new ConnectionSettings(poolA)
                    .DisableAutomaticProxyDetection(false)
                    .RequestTimeout(TimeSpan.FromSeconds(10))
                    .SniffLifeSpan(null);

                var _clientA = new ElasticClient(settingsA);
                var nodes = (
                        await _clientA.Nodes.StatsAsync((r) => r.Metric(Elasticsearch.Net.NodesStatsMetric.Fs))
                        ).Nodes
                        .Select(m => $"http://{m.Value?.Host}:9200")
                        .ToArray();


                HealthStatus status = HealthStatus.Healthy;
                string report = $"Num. of nodes:{nodes.Count()}";
                foreach (var esUrl in nodes)
                {

                    var singlePool = new Elasticsearch.Net.SingleNodeConnectionPool(new Uri(esUrl));

                    var settings = new ConnectionSettings(singlePool)
                        .DefaultIndex(options.IndexName)
                        .DisableAutomaticProxyDetection(false)
                        .RequestTimeout(TimeSpan.FromSeconds(10))
                        .SniffLifeSpan(null)

                        ;

                    var stopwatch = new Devmasters.DT.StopWatchEx();
                    stopwatch.Start();
                    var _client = new ElasticClient(settings);

                    var res = await _client.SearchAsync<object>(s => s
                            .Query(q => q.QueryString(qs => qs.Query(options.Query)))
                            .Size(0)
                            .TrackTotalHits(true)
                        );
                    stopwatch.Stop();

                    List<Tuple<string, long, TimeSpan, string>> nodesQueryStat = new List<Tuple<string, long, TimeSpan, string>>();

                    if (res.IsValid == false)
                    {
                        nodesQueryStat.Add(new Tuple<string, long, TimeSpan, string>(esUrl, -1, stopwatch.Elapsed, res?.ServerError?.ToString()));
                        status = HealthStatus.Unhealthy;
                        report += $"\n{esUrl} error:{res?.ServerError?.ToString()}";
                    }
                    else
                    {
                        nodesQueryStat.Add(new Tuple<string, long, TimeSpan, string>(esUrl, res.Total, stopwatch.Elapsed, res?.ServerError?.ToString()));
                        if (stopwatch.Elapsed > options.ErrorResponseTime)
                        { 
                            status = HealthStatus.Unhealthy;
                            report += $"\n{esUrl} very slow:{stopwatch.ElapsedMilliseconds} ms";
                        }
                        else if (stopwatch.Elapsed > options.WarningResponseTime && status > HealthStatus.Degraded)
                        { 
                            status = HealthStatus.Degraded;
                            report += $"\n{esUrl} slow:{stopwatch.ElapsedMilliseconds} ms";
                        }
                    }

                }
                return new HealthCheckResult(status, description: report);
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }

    }
}
