using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class WithLogging : IHealthCheck
    {
        IHealthCheck origHC = null;
        public WithLogging(IHealthCheck originalHC)
        {
            if (originalHC == null)
                throw new ArgumentNullException("originalHC");
            this.origHC = originalHC;
        }
        ILogger logger = Log.ForContext<WithLogging>();

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            logger.Debug("Starting {healthcheck}", origHC.GetType().FullName);
            var hcRes = await origHC.CheckHealthAsync(context, cancellationToken);
            var description = hcRes.Description;
            HealthCheckResult newHCres = new HealthCheckResult(
                hcRes.Status, description, hcRes.Exception, hcRes.Data
                );
            logger.Debug("Ending {healthcheck} with {@HcStatus}", origHC.GetType().FullName, newHCres);

            return newHCres;
        }
    }
}
