﻿using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
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
        ILogger logger = Log.ForContext<WithResponseTime>();

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
            sw.Start();
            logger.Debug("Starting {healthcheck}", origHC.GetType().FullName);
            var hcRes = await origHC.CheckHealthAsync(context, cancellationToken);
            sw.Stop();
            var description = hcRes.Description + $" Response time {sw.ElapsedMilliseconds} ms.";
            HealthCheckResult newHCres = new HealthCheckResult(
                hcRes.Status, description, hcRes.Exception, hcRes.Data
                );
            logger.Debug("Ending {healthcheck} with {@HcStatus}", origHC.GetType().FullName, newHCres);

            return newHCres;
        }
    }
}
