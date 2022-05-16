using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class VerejneZakazkyZpracovane : IHealthCheck
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
                    var res = await HlidacStatu.Repositories.VerejnaZakazkaRepo.Searching.SimpleSearchAsync(
                        $"lastUpdated:{date:yyyy-MM-dd}", null, 1, 1,
                        "0", exactNumOfResults: true);

                    bool svatek = Devmasters.DT.Util.NepracovniDny[date.Year].Contains(date);
                    result += $"{date:yyyy-MM-dd}: {res.Total} zakázek \n";
                    if (svatek && res.Total < 5)
                        bad = true;
                    else if (res.Total < 30)
                        bad = true;
                }

                if (bad)
                    return HealthCheckResult.Degraded(result);
                else
                    return HealthCheckResult.Healthy(result);
            }
            catch (Exception e)
            {
                return HealthCheckResult.Unhealthy("Unknown status, cannot read data from network disk", e);
            }
        }
    }
}