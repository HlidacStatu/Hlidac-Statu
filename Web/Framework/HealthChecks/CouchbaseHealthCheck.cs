using Couchbase;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Framework.HealthChecks
{
    public class CouchbaseHealthCheck : IHealthCheck
    {
        private Options options;

        public enum Service
        {
            KeyValue,
            Views,
            Query,
            Search,
            Config,
            Analytics
        }

        public class Options
        {
            public string Username { get; set; }
            public string Password { get; set; }
            public string Bucket { get; set; }
            public Service? Service { get; set; }
            public string[] ServerUris { get; set; }
        }

        public CouchbaseHealthCheck(Options options)
        {
            this.options = options;
        }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Cluster cluster = new Cluster(new global::Couchbase.Configuration.Client.ClientConfiguration
            {
                Servers = options.ServerUris.Select(s => new Uri(s)).ToList()
            });
            var authenticator = new global::Couchbase.Authentication.PasswordAuthenticator(
                options.Username,
                options.Password);
            cluster.Authenticate(authenticator);
            var cbucket = cluster.OpenBucket(options.Bucket);

            Couchbase.Core.Monitoring.IPingReport? pingReport = null;
            if (this.options.Service == null)
                pingReport = cbucket.Ping();
            else
                pingReport = cbucket.Ping((Couchbase.Core.Monitoring.ServiceType)options.Service.Value);

            var statuses = pingReport.Services
                .SelectMany(m => m.Value)
                .Where(m => m.State.HasValue)
                .GroupBy(m => m.State.Value);

            if (statuses.All(m => m.Key == Couchbase.Core.Monitoring.ServiceState.Ok || m.Key == Couchbase.Core.Monitoring.ServiceState.Connected))
                return Task.FromResult(HealthCheckResult.Healthy());

            var report = "";
            var delimiter = "\r\n";
            foreach (var status in statuses
                .Where(m=>!(m.Key == Couchbase.Core.Monitoring.ServiceState.Ok || m.Key == Couchbase.Core.Monitoring.ServiceState.Connected))
                )
            {
                foreach (var item in status)
                {
                    report += $"{status.Key}! Service:{item.Type}({item.Remote}){delimiter}";
                }

            }


            return Task.FromResult(HealthCheckResult.Unhealthy(description: report));
        }
    }
}


