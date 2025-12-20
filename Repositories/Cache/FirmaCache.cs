using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class FirmaCache
{
    private static IFusionCache MemoryCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(FirmaCache));

    private static IFusionCache PostgreCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2PostgreSql, nameof(FirmaCache));

    private static IFusionCache MemcachedCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2Memcache, nameof(FirmaCache));
    
    private static IFusionCache PermanentCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore, nameof(FirmaCache));

    public static ValueTask<InfoFact[]> GetInfoFactsAsync(Firma firma) =>
        PostgreCache.GetOrSetAsync($"_InfoFacts:{firma.ICO}",
            _ => FirmaExtension.GetDirectInfoFactsAsync(firma),
            options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6));
                options.ModifyEntryOptionsFactoryTimeouts(
                    factoryHardTimeout: TimeSpan.FromMinutes(30));
            });

    public static ValueTask InvalidateInfoFactsAsync(Firma firma) => PostgreCache.ExpireAsync($"_InfoFacts:{firma.ICO}");

    public static ValueTask<Riziko[]> GetRizikoAsync(Firma f, int rok) =>
        PostgreCache.GetOrSetAsync($"_Rizika:{f.ICO}-{rok}",
            _ => FirmaExtension.GetDirectRizikoAsync(f, rok),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static ValueTask InvalidateRizikoAsync(Firma f, int rok) => PostgreCache.ExpireAsync($"_Rizika:{f.ICO}-{rok}");

    public static ValueTask<Firma.Zatrideni.Item[]> GetSubjektyForOborAsync(Firma.Zatrideni.SubjektyObory obor) =>
        MemcachedCache.GetOrSetAsync($"_SubjektyForObor:{obor:G}",
            _ => FirmaRepo.Zatrideni.GetSubjektyDirectAsync(obor),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromDays(1))
        );

    public static Firma[] Ministerstva() =>
        MemoryCache.GetOrSet("StatData.Ministerstva",
            _ => OvmRepo.Ministerstva().Select(m => Firmy.Get(m.ICO)).ToArray(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static Firma[] VysokeSkoly() =>
        MemoryCache.GetOrSet("StatData.VysokeSkoly",
            _ => OvmRepo.VysokeSkoly().Select(m => Firmy.Get(m.ICO)).ToArray(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static Firma[] KrajskeUrady() =>
        MemoryCache.GetOrSet("StatData.KrajskeUrady",
            _ => OvmRepo.KrajskeUrady().Select(m => Firmy.GetByDS(m.IdDS)).ToArray(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static Dictionary<string, string[]> MestaPodleKraju() =>
        MemoryCache.GetOrSet("StatData.MestaPodleKraju",
            _ => OvmRepo.ObceSRozsirenouPusobnostiPodleKraju(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static Firma[] ObceSRozsirenouPusobnosti() =>
        MemoryCache.GetOrSet("StatData.StatutarniMestaAll",
            _ => OvmRepo.ObceSRozsirenouPusobnosti().Select(m => Firmy.GetByDS(m.IdDS)).ToArray(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );
    
    
    public static HashSet<string> VsechnyStatniMestskeFirmy() =>
        MemoryCache.GetOrSet("StatData.VsechnyStatniMestskeFirmy",
            _ => FirmaVlastnenaStatemRepo.IcaStatnichFirem().ToHashSet(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static HashSet<string> VsechnyStatniMestskeFirmy25percs() =>
        MemoryCache.GetOrSet("StatData.VsechnyStatniMestskeFirmy25percs",
            _ => FirmaVlastnenaStatemRepo.IcaStatnichFirem(25).ToHashSet(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static HashSet<string> UradyOvm() =>
        MemoryCache.GetOrSet("StatData.UradyOvm",
            _ =>
            {
                var urady = OvmRepo.UradyOvm().Select(u => u.ICO).ToHashSet();
                urady.Add("00832227"); //Euroregion Neisse - Nisa - Nysa
                return urady;
            },
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static Dictionary<string, string[]> FirmyNazvyOnlyAscii(bool forceUpdate = false)
    {
        string key = $"_FirmyNazvyOnlyAscii";

        if (forceUpdate)
        {
            PermanentCache.Expire(key);
        }

        return PermanentCache.GetOrSet(key,
            _ => FirmaRepo.GetNazvyOnlyAscii(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365))
        );
    }

    
}

