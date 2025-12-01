using Devmasters;
using Microsoft.Extensions.Diagnostics.HealthChecks;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class WinSystemMonitor : IHealthCheck
    {
        private Options options;

        public class Options
        {
            public string Uri { get; set; }
            public TimeSpan IISLongRequestDuration { get; set; } = TimeSpan.FromSeconds(1);
            public int IISLongRequestsDegradedThreshold { get; set; } = 5;
            public int IISLongRequestsUnhealthyThreshold { get; set; } = 20;
            public long SystemDiskFreeBytesDegradedThreshold { get; set; } = 10L * (1024 * 1024 * 1024); // 10 GB
            public long SystemDiskFreeBytesUnhealthyThreshold { get; set; } = 1L * (1024 * 1024 * 1024); // 10 GB
        }
        public WinSystemMonitor(Options options)
        {
            this.options = options;
        }


        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                List<string> report = new List<string>();
                HlidacStatu.DS.Api.WinSystemMonitorData data = null;

                data = await Devmasters.Net.HttpClient.Simple.GetAsync<DS.Api.WinSystemMonitorData>(options.Uri, timeout: TimeSpan.FromSeconds(2));
                var longRequests = data.IISRequests
                    .Where(r => r.PipelineState == DS.Api.WinSystemMonitorData.IIS.Request.PipelineStateEnum.ExecuteRequestHandler)
                    .Where(r => r.TimeElapsedInMs >= options.IISLongRequestDuration.TotalMilliseconds);

                //format stats
                report.Add($"CPU: {data.CPU}%");
                report.Add($"UsedMemory: {data.UsedMemoryBytes / (1024 * 1024):#.###} MB");
                report.Add($"FreeSystemDisk: {data.SystemDiskFreeBytes / (1024 * 1024 * 1024):0.###} GB");
                report.Add($"IISRequest: {data.IISRequests.Count()} total");
                report.Add($"Long IISRequest: {longRequests.Count()} total");
                if (longRequests.Any())
                {
                    var perAppPool = longRequests
                        .GroupBy(r => r.ApplicationPoolName, v => v, (k, v) => new { apppool = k, requests = v })
                        .OrderBy(k => k.apppool)
                        .SelectMany(g => g.requests.Select((r, n) => $"{n}. AppPool {r.ApplicationPoolName} {r.Url.ShortenMe(30)} [{r.TimeElapsedInMs:### ##0 000} ms]"));
                    report.AddRange(perAppPool);
                }


                string sreport = string.Join("<br/>\n", report);

                if (longRequests.Count() >= options.IISLongRequestsUnhealthyThreshold)
                    return HealthCheckResult.Unhealthy(sreport);

                if (longRequests.Count() >= options.IISLongRequestsDegradedThreshold)
                    return HealthCheckResult.Degraded(sreport);

                return HealthCheckResult.Healthy(sreport);

            }
            catch (Exception ex)
            {
                return new HealthCheckResult(HealthStatus.Unhealthy, exception: ex);
            }
        }

    }
}
