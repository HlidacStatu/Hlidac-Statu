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

    public static ValueTask InvalidateInfoFactsAsync(Firma firma) =>
        PostgreCache.ExpireAsync($"_InfoFacts:{firma.ICO}");

    public static ValueTask<Riziko[]> GetRizikoAsync(Firma f, int rok) =>
        PostgreCache.GetOrSetAsync($"_Rizika:{f.ICO}-{rok}",
            _ => FirmaExtension.GetDirectRizikoAsync(f, rok),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static ValueTask InvalidateRizikoAsync(Firma f, int rok) =>
        PostgreCache.ExpireAsync($"_Rizika:{f.ICO}-{rok}");

    public static ValueTask<Firma.Zatrideni.Item[]> GetSubjektyForOborAsync(Firma.Zatrideni.SubjektyObory obor) =>
        MemcachedCache.GetOrSetAsync($"_SubjektyForObor:{obor:G}",
            _ => FirmaRepo.Zatrideni.GetSubjektyDirectAsync(obor),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromDays(1))
        );

    public static async Task<Firma[]> MinisterstvaAsync() =>
        await MemoryCache.GetOrSetAsync("StatData.Ministerstva",
            async _ =>
            {
                var ministerstva = OvmRepo.Ministerstva();
                var result = new List<Firma>();
                foreach (var m in ministerstva)
                {
                    result.Add(await GetAsync(m.ICO));
                }

                return result.ToArray();
            },
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static ValueTask<Firma[]> VysokeSkolyAsync() =>
        MemoryCache.GetOrSetAsync("StatData.VysokeSkoly",
            async _ =>
            {
                var vysokeSkoly = OvmRepo.VysokeSkoly();
                var result = new List<Firma>();
                foreach (var m in vysokeSkoly)
                {
                    result.Add(await GetAsync(m.ICO));
                }

                return result.ToArray();
            },
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static ValueTask<Firma[]> KrajskeUradyAsync() =>
        MemoryCache.GetOrSetAsync("StatData.KrajskeUrady",
            async _ =>
            {
                var krajskeUrady = OvmRepo.KrajskeUrady();
                var result = new List<Firma>();
                foreach (var k in krajskeUrady)
                {
                    result.Add(await GetByDSAsync(k.IdDS));
                }

                return result.ToArray();
            },
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );


    public static Dictionary<string, string[]> MestaPodleKraju() =>
        MemoryCache.GetOrSet("StatData.MestaPodleKraju",
            _ => OvmRepo.ObceSRozsirenouPusobnostiPodleKraju(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );


    public static ValueTask<Firma[]> ObceSRozsirenouPusobnostiAsync() =>
        MemoryCache.GetOrSetAsync("StatData.StatutarniMestaAll",
            async _ =>
            {
                var obce = OvmRepo.ObceSRozsirenouPusobnosti();
                var result = new List<Firma>();
                foreach (var k in obce)
                {
                    result.Add(await GetByDSAsync(k.IdDS));
                }

                return result.ToArray();
            },
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );


    public static ValueTask<HashSet<string>> VsechnyStatniMestskeFirmyAsync() =>
        MemoryCache.GetOrSetAsync("StatData.VsechnyStatniMestskeFirmy",
            async _ => await FirmaVlastnenaStatemRepo.IcaUraduStatnichFiremAsync(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static ValueTask<HashSet<string>> UradyOvmAsync() =>
        MemoryCache.GetOrSetAsync("StatData.UradyOvm",
            async _ =>
            {
                var urady = await FirmaVlastnenaStatemRepo.IcaOVMAsync();
                urady.Add("00832227"); //Euroregion Neisse - Nisa - Nysa
                return urady;
            },
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6))
        );

    public static async Task<Dictionary<string, string[]>> FirmyNazvyOnlyAsciiAsync(bool forceUpdate = false)
    {
        string key = $"_FirmyNazvyOnlyAscii";

        if (forceUpdate)
        {
            await PermanentCache.ExpireAsync(key);
        }

        return await PermanentCache.GetOrSetAsync(key,
            async _ => await FirmaRepo.GetNazvyOnlyAsciiAsync(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365))
        );
    }

    public static async Task<string> GetJmenoAsync(int ICO)
    {
        return await GetJmenoAsync(ICO.ToString().PadLeft(8, '0'));
    }

    public static async Task<string> GetJmenoAsync(string ico)
    {
        if (string.IsNullOrWhiteSpace(ico))
            return string.Empty;
        if (Util.DataValidators.CheckCZICO(ico) == false)
            return string.Empty;
        var icoNormalized = Util.ParseTools.NormalizeIco(ico);

        return await MemcachedCache.GetOrSetAsync($"_firmaNameOnlyByICO_v4:{icoNormalized}",
            async _ =>
            {
                var o = await FirmaRepo.FromIcoAsync(icoNormalized);
                if (o == null)
                    return icoNormalized;
                if (o.Valid == false)
                    return icoNormalized;
                return o.Jmeno;
            },
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12))
        );
    }

    public static async Task<Firma> GetAsync(int ICO)
    {
        return await GetAsync(ICO.ToString().PadLeft(8, '0'));
    }

    public static async Task<Firma> GetAsync(string ICO)
    {
        if (string.IsNullOrEmpty(ICO))
            return null;

        string normalizedIco = Util.ParseTools.NormalizeIco(ICO);
        if (string.IsNullOrWhiteSpace(normalizedIco))
            return null;

        return await MemcachedCache.GetOrSetAsync($"_firmyByICO_v4:{normalizedIco}",
            async _ => await FirmaRepo.FromIcoAsync(normalizedIco),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(4))
        );
    }

    public static async Task<Firma> GetByDSAsync(string ds)
    {
        if (ds == null)
            ds = string.Empty;

        return await MemcachedCache.GetOrSetAsync($"_firmyByDS_v4:{ds}",
            async _ => await FirmaRepo.FromDSAsync(ds),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(4))
        );
    }
}