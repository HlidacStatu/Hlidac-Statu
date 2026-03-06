using System;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Entities;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class PlatyCache
{
    private static IFusionCache MemoryCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(PlatyCache));

    public static async Task<PuPlatStat> PlatyUrednikuStatisticPerYearAsync(int year) =>
        await MemoryCache.GetOrSetAsync($"_PlatyUrednikuStatisticPerYear:{year}",
            async _ =>
            {
                var data = await PuRepo.GetPlatyAsync(year);
                if (data == null)
                    return new PuPlatStat([]);
                
                var platyStat = new PuPlatStat(data);

                return platyStat;
            },
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );
    
    public static async Task<PpGlobalStat> PlatyPolitikuStatisticPerYearAsync(int year) =>
        await MemoryCache.GetOrSetAsync($"_PlatyPolitikuStatisticPerYear:{year}",
            async _ => await PpRepo.GetGlobalStatAsync(year),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );
}