using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Framework.HealthChecks
{
    public class OCRQueue : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {


            return Task.FromResult(HealthCheckResult.Unhealthy(description: $"The healthcheck {context.Registration.Name} failed at minute {DateTime.UtcNow.Minute}"));
        }
    }
}
