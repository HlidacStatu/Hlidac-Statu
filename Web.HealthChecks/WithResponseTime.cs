using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class WithResponseTime : IHealthCheck
    {
        IHealthCheck origHC = null;
        public WithResponseTime(IHealthCheck originalHC)
        {
            if (originalHC == null)
                throw new ArgumentNullException("originalHC");
            this.origHC = originalHC;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
            sw.Start();
            var hcRes = await origHC.CheckHealthAsync(context, cancellationToken);
            sw.Stop();
            var description = hcRes.Description + $" Response time {sw.ElapsedMilliseconds} ms.";
            HealthCheckResult newHCres = new HealthCheckResult(
                hcRes.Status, description, hcRes.Exception, hcRes.Data
                );

            return newHCres;
        }
    }
}
