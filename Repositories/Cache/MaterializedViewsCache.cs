using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories.Analysis;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public class MaterializedViewsCache
{
    private static IFusionCache PermanentCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore,
            nameof(MaterializedViewsCache));

    //caches without factory
    public static ValueTask<AnalysisCalculation.VazbyFiremNaUradyStat> NespolehlivyPlatciDPH_ObchodySUradyAsync(
        AnalysisCalculation.VazbyFiremNaUradyStat data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<AnalysisCalculation.VazbyFiremNaUradyStat>(
            "NespolehlivyPlatciDPH_ObchodySUrady",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<AnalysisCalculation.VazbyFiremNaUradyStat> UradyObchodujiciSNespolehlivymiPlatciDPHAsync(
        AnalysisCalculation.VazbyFiremNaUradyStat data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<AnalysisCalculation.VazbyFiremNaUradyStat>(
            "UradyObchodujiciSNespolehlivymiPlatciDPH",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_AktualniAsync(
        AnalysisCalculation.VazbyFiremNaPolitiky data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<AnalysisCalculation.VazbyFiremNaPolitiky>(
            "FirmySVazbamiNaPolitiky_Aktualni",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_NedavneAsync(
        AnalysisCalculation.VazbyFiremNaPolitiky data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<AnalysisCalculation.VazbyFiremNaPolitiky>(
            "FirmySVazbamiNaPolitiky_Nedavne",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_VsechnyAsync(
        AnalysisCalculation.VazbyFiremNaPolitiky data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<AnalysisCalculation.VazbyFiremNaPolitiky>(
            "FirmySVazbamiNaPolitiky_Vsechny",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );
    
    // dotace reports
    // to rebuild use Tasks.RebuildStatisticsDotaceAsync()
    public static ValueTask<string[]> AllIcosInDotaceCacheAsync(string[] data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<string[]>(
            "_allIcosInDotaceCache",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<StatisticsSubjectPerYear<Firma.Statistics.Dotace>[]> TopDotaceHoldingCacheAsync(
        StatisticsSubjectPerYear<Firma.Statistics.Dotace>[] data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<StatisticsSubjectPerYear<Firma.Statistics.Dotace>[]>(
            "_topDotaceHoldingCache",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    public static ValueTask<StatisticsSubjectPerYear<Firma.Statistics.Dotace>[]> TopDotaceHoldingStatniCacheAsync(
        StatisticsSubjectPerYear<Firma.Statistics.Dotace>[] data = null)
        => PermanentCache.LoadOrSetDataFromPermanentCacheAsync<StatisticsSubjectPerYear<Firma.Statistics.Dotace>[]>(
            "_topDotaceHoldingStatniCache",
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365)),
            data
        );

    
    
    
    
    
    

    //Caches with factory
    public static async Task<AnalysisCalculation.VazbyFiremNaUradyStat>
        GetUradyObchodujiciSFirmami_s_vazbouNaPolitiky_vsechnyAsync(bool forceUpdate = false)
    {
        string key = $"UradyObchodujiciSFirmami_s_vazbouNaPolitiky_vsechny";

        if (forceUpdate)
        {
            await PermanentCache.ExpireAsync(key);
        }

        return await PermanentCache.GetOrSetAsync(key,
            _ => AnalysisCalculation
                .UradyObchodujiciSFirmami_s_vazbouNaPolitikyAsync(Relation.AktualnostType.Libovolny, true),
            options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365));
            });
    }

    public static async Task<AnalysisCalculation.VazbyFiremNaUradyStat>
        GetUradyObchodujiciSFirmami_s_vazbouNaPolitiky_nedavneAsync(bool forceUpdate = false)
    {
        const string key = "UradyObchodujiciSFirmami_s_vazbouNaPolitiky_nedavne";

        if (forceUpdate)
            await PermanentCache.ExpireAsync(key);

        return await PermanentCache.GetOrSetAsync(key,
            _ => AnalysisCalculation.UradyObchodujiciSFirmami_s_vazbouNaPolitikyAsync(Relation.AktualnostType.Nedavny,
                true),
            options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365));
            });
    }

    public static async Task<AnalysisCalculation.VazbyFiremNaUradyStat>
        GetUradyObchodujiciSFirmami_s_vazbouNaPolitiky_aktualniAsync(bool forceUpdate = false)
    {
        const string key = "UradyObchodujiciSFirmami_s_vazbouNaPolitiky_aktualni";

        if (forceUpdate)
            await PermanentCache.ExpireAsync(key);

        return await PermanentCache.GetOrSetAsync(key,
            _ => AnalysisCalculation.UradyObchodujiciSFirmami_s_vazbouNaPolitikyAsync(Relation.AktualnostType.Aktualni,
                true),
            options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365));
            });
    }

    public static async Task<IEnumerable<AnalysisCalculation.IcoSmlouvaMinMax>> GetFirmyCasovePodezreleZalozeneAsync(
        IEnumerable<AnalysisCalculation.IcoSmlouvaMinMax> data = null)
    {
        const string key = "FirmyCasovePodezreleZalozene";

        if (data != null)
        {
            await PermanentCache.SetAsync(key, data, options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365));
            });
        }

        return await PermanentCache.GetOrSetAsync(key,
            _ => AnalysisCalculation.GetFirmyCasovePodezreleZalozeneAsync(),
            options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365));
            });
    }

    public static async Task<string> GetCzechDictAsync(bool forceUpdate = false)
    {
        const string key = "Czech.3-2-5.dic.txt";

        if (forceUpdate)
            await PermanentCache.ExpireAsync(key);

        return await PermanentCache.GetOrSetAsync(key,
            _ => Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/Czech.3-2-5.dic.txt"),
            options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromDays(10 * 365), TimeSpan.FromDays(10 * 365));
            });
    }

    public static string GetCrawlerUserAgents(bool forceUpdate = false)
    {
        const string key = "crawler-user-agents.json";

        if (forceUpdate)
            PermanentCache.Expire(key);

        return PermanentCache.GetOrSet(key,
            _ => Devmasters.Net.HttpClient.Simple.Get("https://somedata.hlidacstatu.cz/appdata/crawler-user-agents.json"),
            options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromDays(10 * 365), TimeSpan.FromDays(10 * 365));
            });
    }
}