using Couchbase;

using HlidacStatu.Lib.Data.External.Camelot;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class CamelotApis : IHealthCheck
    {

        private Options options;

        public class Options
        {
            public string[] CamelotAPIUris { get; set; }
        }

        public CamelotApis(Options options)
        {
            this.options = options;
        }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);
            try
            {
                foreach (var url in options.CamelotAPIUris)
                {
                    using (ClientLow cl = new ClientLow(new SingleConnection(url), "", ClientLow.Commands.lattice))
                    {
                        var ver = cl.VersionAsync().Result;
                        var st = cl.StatisticAsync().Result;
                        sb.AppendLine($"{url} ({ver.Data?.apiVersion}/{ver.Data?.camelotVersion})");
                        sb.AppendLine($"   {st.Data?.CurrentThreads}/{st.Data?.MaxThreads}, parsed {st.Data?.ParsedFiles}");
                        sb.AppendLine();
                    }

                }
                return Task.FromResult(HealthCheckResult.Healthy(sb.ToString()));

            }
            catch (Exception e)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(exception: e));
            }

        }
    }
}


