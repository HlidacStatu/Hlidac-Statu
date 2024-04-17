
using Devmasters.Cache;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Org.BouncyCastle.Bcpg.Sig;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HlidacStatu.Web.HealthChecks
{
    public class DevmastersCacheCheck : IHealthCheck
    {
        private readonly BaseCache<string> cache;

        public DevmastersCacheCheck(Devmasters.Cache.BaseCache<string> cacheInstance)
        {
            if (cacheInstance == null)
                throw new ArgumentNullException("cacheInstance");
            this.cache = cacheInstance;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {

            //string prefix = "DevmastersCacheCheck_test_" + Devmasters.TextUtil.GenRandomString(5);
            try
            {

                var x = this.cache.Get();
                await Task.Delay(20);
                var x2 = this.cache.Get();
                if (x != x2)
                    return HealthCheckResult.Unhealthy("Different cache content");

                string newContent = "new content";
                this.cache.ForceRefreshCache(newContent);
                x = this.cache.Get();
                if (x != newContent)
                    return HealthCheckResult.Unhealthy("Different cache new content");

                var exists = this.cache.Exists();
                if (!exists)
                    return HealthCheckResult.Unhealthy("Cache doesn't exists");

                this.cache.Invalidate();
                exists = this.cache.Exists();
                if (exists)
                    return HealthCheckResult.Unhealthy("Cache should be deleted");

                return HealthCheckResult.Healthy($"Cache {this.cache.GetType().Name} is OK.");
            }
            catch (Exception e)
            {
                return HealthCheckResult.Degraded("Unknown status, cannot read data from network disk", e);
            }


        }

    }
}


