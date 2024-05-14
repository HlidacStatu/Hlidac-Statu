using System;
using ZiggyCreatures.Caching.Fusion;

namespace PlatyUredniku.Cache;

public static class CachingOptions
{
    // Čerstvá data má v paměti po dobu 10 minut => stará
    // Stará data má v paměti max. 4 hodiny a pokud dojde k výpadku zdroje, použije starou hodnotu a prodlouží dočasnou čerstvost o 1 minutu
    // Při načítání nových dat do cache dá databázi 100ms čas a v případě, že požadavek trvá déle, použije starou cache, zatímco na pozadí načítá data nová
    public static readonly FusionCacheEntryOptions Default = new FusionCacheEntryOptions(
            //TODO put back before release TimeSpan.FromMinutes(10)
            TimeSpan.FromSeconds(10)
        )
        .SetFailSafe(true, TimeSpan.FromHours(4), TimeSpan.FromMinutes(1)) 
        .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100)
        );

}