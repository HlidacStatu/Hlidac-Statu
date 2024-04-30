using System;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Util;

public static class CachingOptions
{
    /// <summary>
    /// Čerstvá data má v paměti po dobu 10 minut => stará 
    /// Stará data má v paměti max. 4 hodiny a pokud dojde k výpadku zdroje, použije starou hodnotu a prodlouží dočasnou čerstvost o 1 minutu
    /// Při načítání nových dat do cache dá databázi 100ms čas a v případě, že požadavek trvá déle, použije starou cache, zatímco na pozadí načítá data nová  
    /// </summary>
    public static readonly FusionCacheEntryOptions Cache10m_failsave4h = new FusionCacheEntryOptions(TimeSpan.FromMinutes(10))
        .SetFailSafe(true, TimeSpan.FromHours(4), TimeSpan.FromMinutes(1)) 
        .SetFactoryTimeouts(TimeSpan.FromMilliseconds(100)
        );
    
    /// <summary>
    /// Jednoduchá cache, která po 1 minutě zahodí data.
    /// </summary>
    public static readonly FusionCacheEntryOptions Simple1m = new(TimeSpan.FromMinutes(1));

}