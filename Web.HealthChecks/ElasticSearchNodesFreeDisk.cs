using Microsoft.Extensions.Diagnostics.HealthChecks;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class ElasticSearchNodesFreeDisk : IHealthCheck
    {
        private Options options;

        public class Options
        {
            public string[] ElasticServerUris { get; set; }
            public int? ExpectedNumberOfNodes { get; set; } = null;
            public int? MinimumFreeSpaceInMegabytes { get; set; }
            public decimal? MinimumFreeSpaceInPercent { get; set; }
        }
        public ElasticSearchNodesFreeDisk(Options options)
        {
            this.options = options;
            if (this.options.MinimumFreeSpaceInMegabytes.HasValue == false && this.options.MinimumFreeSpaceInPercent.HasValue == false)
                throw new System.ArgumentException("options.MinimumFreeSpaceInMegabytes or options.MinimumFreeSpaceInPercent must have value.");
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

                var res = await _client.Nodes.StatsAsync((r) => r.Metric(Elasticsearch.Net.NodesStatsMetric.Fs));
                //res = await _client.Nodes.StatsAsync((r) => r.Metric(Elasticsearch.Net.NodesStatsMetric.All));

                List<Tuple<string, Nest.FileSystemStats.TotalFileSystemStats>> nodesStat = new List<Tuple<string, Nest.FileSystemStats.TotalFileSystemStats>>();

                if (res.IsValid == false)
                    return new HealthCheckResult(context.Registration.FailureStatus, exception: new Exception(res?.ServerError?.ToString()));
                else
                {
                    nodesStat = res.Nodes
                        .Select(m => new Tuple<string, Nest.FileSystemStats.TotalFileSystemStats>(m.Value.Name, m.Value?.FileSystem?.Total))
                        .ToList();
                }


                if (res.NodeStatistics.Failed > 0)
                {
                    var failureRes = string.Join(' ',
                        res.NodeStatistics.Failures.Select(m => $"{(m.AdditionalProperties.ContainsKey("node_id") ? m.AdditionalProperties["node_id"] + ":" : "")} {m.ToString()}\n")
                        );
                    return new HealthCheckResult(context.Registration.FailureStatus, description: failureRes);

                }


                //(decimal)m.Value.FileSystem.Total.FreeInBytes / (decimal)m.Value.FileSystem.Total.TotalInBytes

                var numberOfNodes = res?.NodeStatistics?.Total ?? 0;

                string report = $"Num. of nodes:{numberOfNodes}";
                if (options.ExpectedNumberOfNodes.HasValue && options.ExpectedNumberOfNodes.Value != numberOfNodes)
                    report = report + $", {options.ExpectedNumberOfNodes} expected.";
                else
                    report = report + ".";

                Func<Tuple<string, Nest.FileSystemStats.TotalFileSystemStats>, bool> isOKTest = null;
                if (options.MinimumFreeSpaceInMegabytes.HasValue)
                    isOKTest = new Func<Tuple<string, FileSystemStats.TotalFileSystemStats>, bool>(m =>
                        (m.Item2.AvailableInBytes / (1024 * 1024)) > options.MinimumFreeSpaceInMegabytes);
                else
                    isOKTest = new Func<Tuple<string, FileSystemStats.TotalFileSystemStats>, bool>(m =>
                        ((decimal)m.Item2.AvailableInBytes / (decimal)m.Item2.TotalInBytes) > (options.MinimumFreeSpaceInPercent / 100));

                report += $" Total available {nodesStat.Sum(m => m.Item2.AvailableInBytes) / (1024 * 1024 * 1024):N0}GB from {nodesStat.Sum(m => m.Item2.TotalInBytes) / (1024 * 1024 * 1024):N0}GB.";

                if (nodesStat.All(isOKTest))
                    return new HealthCheckResult(HealthStatus.Healthy, description: report);

                report += "\n\n";
                foreach (var item in nodesStat.Where(m => !isOKTest(m)))
                {
                    report += $"{item.Item1} free {item.Item2.AvailableInBytes / (1024 * 1024)}MB ({((decimal)item.Item2.AvailableInBytes / (decimal)item.Item2.TotalInBytes):P1})\n";
                }
                return new HealthCheckResult(context.Registration.FailureStatus, description: report);
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }

    }
}
