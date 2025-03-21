﻿using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class SmlouvyZpracovane : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                string result = "";
                bool bad = false;
                for (int i = 1; i <= 3; i++)
                {
                    DateTime date = DateTime.Now.Date.AddDays(-1 * i);
                    var res = await HlidacStatu.Repositories.SmlouvaRepo.Searching.SimpleSearchAsync(
                        $"zverejneno:{date:yyyy-MM-dd}", 1, 1,
                        Repositories.SmlouvaRepo.Searching.OrderResult.FastestForScroll, exactNumOfResults: true);

                    bool svatek = Devmasters.DT.Util.NepracovniDny[date.Year].Contains(date);
                    result += $"{date:yyyy-MM-dd}: {res.Total} smluv \n";
                    if (svatek && res.Total < 10)
                        bad = true;
                    else if (svatek == false && res.Total < 1000)
                        bad = true;
                }

                if (bad)
                    return HealthCheckResult.Degraded(result);
                else
                    return HealthCheckResult.Healthy(result);
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy("Unknown status, cannot read data", e);
            }
        }
    }
}