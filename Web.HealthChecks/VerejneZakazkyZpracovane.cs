
using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace HlidacStatu.Web.HealthChecks
{
    public class VerejneZakazkyZpracovane : IHealthCheck
    {

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {

            try
            {
                string result = "";
                bool bad = false;
                for (int i = 1; i <= 3; i++)
                {
                    DateTime date = DateTime.Now.Date.AddDays(-1 * i);
                    var res = HlidacStatu.Repositories.VerejnaZakazkaRepo.Searching.SimpleSearch($"lastUpdated:{date:yyyy-MM-dd}",null, 1, 1,
                        "0",exactNumOfResults: true);

                    bool svatek = Devmasters.DT.Util.NepracovniDny[date.Year].Contains(date);
                    result += $"{date:yyyy-MM-dd}: {res.Total} smluv \n";
                    if (svatek && res.Total < 10)
                        bad = true;
                    else if (res.Total < 200)
                        bad = true;

                }
                if (bad)
                    return Task.FromResult(HealthCheckResult.Degraded(result));
                else
                    return Task.FromResult(HealthCheckResult.Healthy(result));

            }
            catch (Exception e)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Unknown status, cannot read data from network disk",e));
            }


        }




    }
}


