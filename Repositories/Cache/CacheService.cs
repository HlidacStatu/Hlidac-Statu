using System;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public class CacheService
{
    public static readonly IFusionCache Cache = new FusionCache(new FusionCacheOptions()
    {
        DefaultEntryOptions = PredefinedCachingOptions.Default,
        CacheName = FusionCacheOptions.DefaultCacheName
    });
    
    public static class PredefinedCachingOptions
    {
        // Čerstvá data má v paměti po dobu 10 minut => stará
        // Stará data má v paměti max. 4 hodiny a pokud dojde k výpadku zdroje, použije starou hodnotu a prodlouží dočasnou čerstvost o 1 minutu
        // Při načítání nových dat do cache dá databázi 100ms čas a v případě, že požadavek trvá déle, použije starou cache, zatímco na pozadí načítá data nová
        public static readonly FusionCacheEntryOptions Default = new FusionCacheEntryOptions(
                TimeSpan.FromMinutes(10)
            )
            .SetFailSafe(true, TimeSpan.FromHours(4), TimeSpan.FromMinutes(1))
            .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100)
            );

        public static readonly FusionCacheEntryOptions Long = new FusionCacheEntryOptions(
                TimeSpan.FromHours(10)
            )
            .SetFailSafe(true, TimeSpan.FromHours(40), TimeSpan.FromMinutes(30))
            .SetFactoryTimeouts(TimeSpan.FromSeconds(10)
            );
    }
}