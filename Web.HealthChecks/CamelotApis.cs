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
                foreach (var url in options.CamelotAPIUris.Distinct())
                {
                    if (Uri.TryCreate(url, UriKind.Absolute, out var xxx))
                    {
                        using (ClientLow cl = new ClientLow(url))
                        {
                            var ver = cl.VersionAsync().Result;
                            if (ver.Success)
                                sb.AppendLine($"{url} ({ver.Data?.apiVersion}/{ver.Data?.camelotVersion})");
                            else
                                sb.AppendLine($"{url} ({ver.ErrorCode}:{ver.ErrorDescription})");
                            var st = cl.StatisticAsync().Result;
                            if (st.Success)
                                sb.AppendLine($"   {st.Data?.CurrentThreads}/{st.Data?.MaxThreads}, parsed {st.Data?.ParsedFiles}");
                            else
                                sb.AppendLine($" ({st.ErrorCode}:{st.ErrorDescription})");

                            sb.AppendLine();
                        }
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


