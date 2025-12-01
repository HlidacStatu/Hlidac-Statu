using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Caching
{
    public static class Extensions
    {

        public static FusionCacheEntryOptions ModifyEntryOptionsFactoryTimeouts(this FusionCacheEntryOptions options,
            TimeSpan? factoryHardTimeout = null, TimeSpan? factorySoftTimeout = null)
        {
            if (factoryHardTimeout.HasValue)
                options.FactoryHardTimeout = factoryHardTimeout.Value;

            if (factorySoftTimeout.HasValue)
                options.FactorySoftTimeout = factorySoftTimeout.Value;

            return options;
        }

        public static FusionCacheEntryOptions ModifyEntryOptionsDuration(this FusionCacheEntryOptions options, 
            TimeSpan inMemoryDuration, TimeSpan? distributedDuration)
        {
            options.Duration = inMemoryDuration;
            options.FailSafeMaxDuration = inMemoryDuration * 4;

            if (distributedDuration is not null)
            {
                if (options.DistributedCacheDuration is not null)
                {
                    options.DistributedCacheDuration = distributedDuration;
                }
                options.DistributedCacheFailSafeMaxDuration = distributedDuration * 4;
            }
            else
            {
                options.DistributedCacheFailSafeMaxDuration = inMemoryDuration * 4 * 4;
            }
            return options;
        }
        
        
    }
}
