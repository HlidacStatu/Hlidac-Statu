
using HlidacStatu.Lib.Data.External.Tables.Camelot;

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

        public class Options : IHCConfig
        {
            public string DefaultSectionName => "Camelot.Services";
            public class Endpoint { 
                public string Uri { get; set; }
                public string ApiKey { get; set; }
            }
            public Endpoint[] Uris { get; set; }

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
                foreach (var endp in options.Uris)
                {
                    if (Uri.TryCreate(endp.Uri, UriKind.Absolute, out var xxx))
                    {
                        using (ClientLow cl = new ClientLow(endp.Uri, endp.ApiKey))
                        {
                            Uri uri = new Uri(endp.Uri);
                            string anonUrl = string.Join("", uri.Host.TakeLast(7)) + ":" + uri.Port;
                            var ver = cl.VersionAsync().Result;
                            if (ver.Success)
                                sb.AppendLine($"{anonUrl} ({ver.Data?.DockerVersion}/{ver.Data?.CamelotVersion})");
                            else
                                sb.AppendLine($"{anonUrl} ({ver.ErrorCode}:{ver.ErrorDescription})");
                            var st = cl.StatisticAsync().Result;
                            if (st.Success)
                                sb.AppendLine($"stats: threads {st.Data?.CurrentThreads}/{st.Data?.MaxThreads}, {HlidacStatu.Util.RenderData.NiceNumber(st.Data?.ParsedFilesTotal ?? 0)} parsed (1H:{HlidacStatu.Util.RenderData.NiceNumber(st.Data?.ParsedFilesIn1H ?? 0)}/24H:{HlidacStatu.Util.RenderData.NiceNumber(st.Data?.ParsedFilesIn24H ?? 0)}), {HlidacStatu.Util.RenderData.NiceNumber(st.Data?.CallsTotal ?? 0)} (1H:{HlidacStatu.Util.RenderData.NiceNumber(st.Data?.CallsIn1H ?? 0)}/24H:{HlidacStatu.Util.RenderData.NiceNumber(st.Data?.CallsIn24H ?? 0)}) api calls, {st.Data?.FilesOnDisk} files {((st.Data?.FilesOnDiskSize ?? 0) / (1024m * 1024m)):N3} MB");
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


