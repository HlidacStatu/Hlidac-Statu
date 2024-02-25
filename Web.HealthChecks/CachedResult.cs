using Microsoft.Extensions.Diagnostics.HealthChecks;

using System;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class CachedResult : IHealthCheck
    {
        IHealthCheck origHC = null;
        private readonly TimeSpan cacheTime;
        Devmasters.Cache.LocalMemory.Cache<Tuple<HealthCheckResult>> cache = null;

        public CachedResult(IHealthCheck originalHC, TimeSpan cacheTime)
        {
            if (originalHC == null)
                throw new ArgumentNullException("originalHC");
            this.origHC = originalHC;
            this.cacheTime = cacheTime;
            
        }

        object lockObj = new object();
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            lock (lockObj)
            {
                if (this.cache == null)
                {
                  this.cache =new Devmasters.Cache.LocalMemory.Cache<Tuple<HealthCheckResult>>(this.cacheTime,
                                    (_) =>
                                    {
                                        Tuple<HealthCheckResult> ret = new Tuple<HealthCheckResult>(
                                            origHC.CheckHealthAsync(context, cancellationToken).ConfigureAwait(false).GetAwaiter().GetResult()
                                            );
                                        return ret;
                                    }
                                    );
                }
            }

            var cacheRes = this.cache.Get();
            HealthCheckResult hcRes = cacheRes.Item1;
            var description = hcRes.Description;
            HealthCheckResult newHCres = new HealthCheckResult(
                hcRes.Status, description, hcRes.Exception, hcRes.Data
                );

            return newHCres;
        }
    }
}
