using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static FusionCacheEntryOptions ModifyEntryOptionsDuration(this FusionCacheEntryOptions options, TimeSpan mainDuration)
        {
            options.Duration = mainDuration;
            options.FailSafeMaxDuration = mainDuration * 4;
            options.DistributedCacheFailSafeMaxDuration = mainDuration * 4 * 4;
            return options;
        }
    }
}
