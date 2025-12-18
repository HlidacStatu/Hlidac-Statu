using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Web.HealthChecks
{
    public class CachedResult : IHealthCheck
    {
        private static readonly object _memoryCachelock = new object();
        private static IFusionCache _memoryCache;
        private static IFusionCache MemoryCache
        {
            get
            {
                if (_memoryCache == null)
                {
                    lock (_memoryCachelock)
                    {
                        _memoryCache ??= HlidacStatu.Caching.CacheFactory.CreateNew(
                            CacheFactory.CacheType.L1Default,
                            "HealthChecksCachedResult");
                    }
                }

                return _memoryCache;
            }
        }
        
        private ValueTask<HealthCheckResult> GetPoskytovateleCacheAsync(HealthCheckContext context, CancellationToken cancellationToken) => MemoryCache.GetOrSetAsync(
            $"_HealthChecksCachedResult:{_healthCheckName}", async ct =>
            {
                var ret = await origHC.CheckHealthAsync(context, ct);
                
                return ret;
                
            }, options =>
            {
                options.Duration = TimeSpan.FromHours(4);
                options.FailSafeMaxDuration = TimeSpan.FromHours(6);
            },
            token: cancellationToken
        );
        
        IHealthCheck origHC = null;
        private readonly TimeSpan cacheTime;
        private readonly string _healthCheckName;

        public CachedResult(IHealthCheck originalHC, TimeSpan cacheTime, string healthCheckName)
        {
            if(string.IsNullOrWhiteSpace(healthCheckName))
                throw new ArgumentNullException(nameof(healthCheckName));
            if (originalHC == null)
                throw new ArgumentNullException(nameof(originalHC));
            this.origHC = originalHC;
            this.cacheTime = cacheTime;
            _healthCheckName = healthCheckName;
        }

        ILogger logger = Log.ForContext<CachedResult>();
        
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            
            logger.Debug("Starting {healthcheck}", origHC.GetType().FullName);
            var cacheRes = await GetPoskytovateleCacheAsync(context, cancellationToken);
            HealthCheckResult hcRes = cacheRes;
            var description = hcRes.Description;
            HealthCheckResult newHCres = new HealthCheckResult(
                hcRes.Status, description, hcRes.Exception, hcRes.Data
            );

            logger.Debug("Ending {healthcheck} with {@HcStatus}", origHC.GetType().FullName, newHCres);
            return newHCres;
        }
    }
}