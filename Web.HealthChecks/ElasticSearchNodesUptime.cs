using Microsoft.Extensions.Diagnostics.HealthChecks;

using Nest;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class ElasticSearchNodesUptime : IHealthCheck
    {
        private Options options;

        public class Options
        {
            public string[] ElasticServerUris { get; set; }
            public int? ExpectedNumberOfNodes { get; set; } = null;
            public TimeSpan MinimumUptime { get; set; } = TimeSpan.FromDays(1);
            public HealthStatus StatusWhenNew { get; set; } = HealthStatus.Healthy;
        }
        public ElasticSearchNodesUptime(Options options)
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

                var res = await _client.LowLevel.Cat.NodesAsync<Elasticsearch.Net.StringResponse>(
                        new Elasticsearch.Net.Specification.CatApi.CatNodesRequestParameters()
                        {
                            Headers = new[] { "m", "name", "u" },
                            SortByColumns = new[] { "name" },
                            Verbose = true
                        }
                    );

                if (res.Success == false)
                    return new HealthCheckResult(context.Registration.FailureStatus, exception: res?.OriginalException);

                return new HealthCheckResult(context.Registration.FailureStatus, description: res.Body);

            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }

    }
}
