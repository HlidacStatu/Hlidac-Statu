using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
using ZiggyCreatures.Caching.Fusion;
using HlidacStatu.Connectors;


namespace HlidacStatu.Repositories.Cache;

public static class KindexCache
{
    private static readonly object _permanentCacheLock = new();
    private static IFusionCache _permanentCache;
    private static IFusionCache PermanentCache
    {
        get
        {
            if (_permanentCache == null)
            {
                lock (_permanentCacheLock)
                {
                    _permanentCache ??= HlidacStatu.Caching.CacheFactory.CreateNew(
                        CacheFactory.CacheType.PermanentStore,
                        nameof(KindexCache));
                }
            }

            return _permanentCache;
        }
    }

    private static readonly object _memoryCachelock = new object();
    private static IFusionCache _memoryCache;
    private static IFusionCache MemoryCache
    {
        get
        {
            if (_memoryCache == null)
            {
                lock (_memoryCachelock)
                {
                    _memoryCache ??= HlidacStatu.Caching.CacheFactory.CreateNew(
                        CacheFactory.CacheType.L1Default,
                        nameof(KindexCache));
                }
            }

            return _memoryCache;
        }
    }


    public static ValueTask<HashSet<string>> GetKindexReadyIcosAsync() =>
        MemoryCache.GetOrSetAsync($"_KIndexReadyIcos", async _ =>
                (await HlidacStatu.Repositories.Searching.Tools.GetAllSmlouvyIdsAsync(
                    Manager.GetESClient_KIndex(),
                    4,
                    "roky.kIndexReady:true"
                )).Result.ToHashSet(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12))
        );


    public static ValueTask<Dictionary<string, SubjectNameCache>> GetKindexCompaniesAsync() =>
        PermanentCache.GetOrSetAsync($"_KIndexCompanies", _ => ListCompaniesAsync(),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6), TimeSpan.FromDays(10 * 365))
        );

    public static ValueTask InvalidateKindexCompaniesAsync() => PermanentCache.ExpireAsync($"_KIndexCompanies");

    public static ValueTask<KIndexData> GetKindexCachedAsync(string ico, bool useTempDb) =>
        MemoryCache.GetOrSetAsync($"_KIndexData:{ico}-{useTempDb}", async _ =>
            {
                KIndexData f = await KIndexRepo.GetDirectAsync(ico, useTempDb);
                if (f == null || f.Ico == "-")
                    return null;
                return f;
            },
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(28))
        );

    public static ValueTask InvalidateKindexCachedAsync(string ico, bool useTempDb) =>
        MemoryCache.ExpireAsync($"_KIndexData:{ico}-{useTempDb}");


    public static ValueTask<KIndexData.KIndexParts[]> GetKindexOrderedValuesFromBestForInfofactsAsync(
        KIndexData.Annual annual, string ico) =>
        MemoryCache.GetOrSetAsync($"_orderedValuesFromBestForInfofacts:{annual.Ico}-{annual.Rok}-{ico}",
            async _ => await OrderedValuesFromBestForInfofactsAsync(annual, ico),
            options => options.ModifyEntryOptionsDuration(TimeSpan.FromDays(2))
        );

    private static async Task<KIndexData.KIndexParts[]> OrderedValuesFromBestForInfofactsAsync(
        this KIndexData.Annual annual, string ico)
    {
        if (annual._orderedValuesForInfofacts == null)
        {
            await annual.AnnualSemaphoreSlim.WaitAsync();
            try
            {
                if (annual._orderedValuesForInfofacts == null)
                {
                    var stat = await HlidacStatu.Repositories.Analysis.KorupcniRiziko.Statistics
                        .GetStatisticsAsync(annual.Rok); //todo: může být null, co s tím?
                    if (annual.KIndexVypocet.Radky != null || annual.KIndexVypocet.Radky.Count() > 0)

                        annual._orderedValuesForInfofacts = annual.KIndexVypocet.Radky
                            .Select(m => new { r = m, rank = stat.SubjektRank(ico, m.VelicinaPart) })
                            .Where(m => m.rank.HasValue)
                            .Where(m =>
                                    m.r.VelicinaPart !=
                                    KIndexData.KIndexParts.PercNovaFirmaDodavatel //nezajimava oblast
                                    && !(m.r.VelicinaPart == KIndexData.KIndexParts.PercSmlouvyPod50kBonus &&
                                         m.r.Hodnota == 0) //bez bonusu
                            )
                            .OrderBy(m => m.rank)
                            .ThenBy(o => o.r.Hodnota)
                            .Select(m => m.r.VelicinaPart)
                            .ToArray(); //better debug
                    else
                        annual._orderedValuesForInfofacts = new KIndexData.KIndexParts[] { };
                }
            }
            finally
            {
                annual.AnnualSemaphoreSlim.Release();
            }
        }

        return annual._orderedValuesForInfofacts;
    }

    private static async Task<Dictionary<string, SubjectNameCache>> ListCompaniesAsync()
    {
        Dictionary<string, SubjectNameCache> companies = new Dictionary<string, SubjectNameCache>();
        await foreach (var kindexRecord in KIndex.YieldExistingKindexesAsync())
        {
            companies.Add(kindexRecord.Ico, new SubjectNameCache(kindexRecord.Jmeno, kindexRecord.Ico));
        }

        return companies;
    }
}