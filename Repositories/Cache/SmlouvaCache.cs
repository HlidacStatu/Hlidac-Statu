using System;
using System.Text;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class SmlouvaCache
{
    private static IFusionCache PermanentCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore, nameof(SmlouvaCache));
    
    private static ValueTask<byte[]> GetRawStemsFromCacheAsync(string smlouvaId) =>
        PermanentCache.GetOrSetAsync($"_SmlouvyStems:{smlouvaId}",
            _ => SmlouvaRepo.GetRawStemsFromServerAsync(smlouvaId),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365))
        );

    private static ValueTask RemoveRawStemsFromCacheAsync(string smlouvaId) =>
        PermanentCache.RemoveAsync($"_SmlouvyStems:{smlouvaId}");

    public static ValueTask SetRawStemsFromCacheAsync(string smlouvaId, byte[] data) =>
        PermanentCache.SetAsync($"_SmlouvyStems:{smlouvaId}", data,
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365))
        );
    public static async Task<bool> ExistsRawStemsFromCacheAsync(string smlouvaId)
    {
        var data = await PermanentCache.GetOrDefaultAsync<byte[]>($"_SmlouvyStems:{smlouvaId}", null,
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365))
        );
        return data != null;
    }

    public static async Task<string> GetRawStemsAsync(string smlouvaId, bool rewriteStems = false)
    {
        if (string.IsNullOrEmpty(smlouvaId))
            return null;
        if (rewriteStems)
        {
            await RemoveRawStemsFromCacheAsync(smlouvaId);
        }

        var data = await GetRawStemsFromCacheAsync(smlouvaId);
        if (data == null)
            return null;

        return Encoding.UTF8.GetString(data);
    }
}