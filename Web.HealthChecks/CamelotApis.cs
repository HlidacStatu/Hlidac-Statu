using HlidacStatu.Lib.Data.External.Tables.Camelot;

using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
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
            public class Endpoint
            {
                public string Uri { get; set; }
                public string ApiKey { get; set; }
                public string Name { get; set; }
            }
            public Endpoint[] Uris { get; set; }

        }

        public CamelotApis(Options options)
        {
            this.options = options;
        }
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder(1024);
            try
            {
                foreach (var endp in options.Uris)
                {
                    if (Uri.TryCreate(endp.Uri, UriKind.Absolute, out var xxx))
                    {
                        await using (ClientLow cl = new ClientLow(endp.Uri, endp.ApiKey))
                        {
                            Uri uri = new Uri(endp.Uri);
                            string anonUrl = endp.Name;
                            if (string.IsNullOrEmpty(anonUrl))
                                anonUrl = string.Join("", uri.Host.TakeLast(7)) + ":" + uri.Port;

                            var ver = await cl.VersionAsync();
                            if (ver.Success)
                                sb.AppendLine($"{anonUrl} ({ver.Data?.AppVersion}/{ver.Data?.CamelotVersion})");
                            else
                                sb.AppendLine($"{anonUrl} ({ver.ErrorCode}:{ver.ErrorDescription})");
                            var st = await cl.StatisticAsync();
                            if (st.Success)
                                sb.AppendLine($"stats: threads {st.Data?.CurrentThreads}/{st.Data?.MaxThreads},"
                                    + $" {HlidacStatu.Util.RenderData.NiceNumber(st.Data?.ParsedFilesTotal ?? 0)} parsed (1H:{HlidacStatu.Util.RenderData.NiceNumber(st.Data?.ParsedFiles1H ?? 0)}/24H:{HlidacStatu.Util.RenderData.NiceNumber(st.Data?.ParsedFiles24H ?? 0)}),"
                                    + $" {HlidacStatu.Util.RenderData.NiceNumber(st.Data?.CallsTotal ?? 0)} ({HlidacStatu.Util.RenderData.NiceNumber(st.Data?.CallsIn1H ?? 0)} / {HlidacStatu.Util.RenderData.NiceNumber(st.Data?.CallsIn24H ?? 0)}) api calls,"
                                    + $" {HlidacStatu.Util.RenderData.NiceNumber(st.Data?.ErrorsTotal ?? 0)} errors ({Devmasters.TextUtil.ShortenText( st.Data?.LastErrorException ??"",50)}),"
                                    + $" {st.Data?.FilesOnDisk} files {((st.Data?.FilesOnDiskSize ?? 0) / (1024m * 1024m)):N3} MB");
                            else
                                sb.AppendLine($" ({st.ErrorCode}:{st.ErrorDescription})");

                            sb.AppendLine();
                        }
                    }
                }
                return HealthCheckResult.Healthy(sb.ToString());

            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy(exception: e);
            }

        }
    }
}


