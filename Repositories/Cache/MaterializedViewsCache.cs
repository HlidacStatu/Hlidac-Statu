using System;
using System.Threading.Tasks;
using HlidacStatu.Caching;
using HlidacStatu.Repositories.Analysis;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public class MaterializedViewsCache
{
    private static readonly IFusionCache PermanentCache =
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore,
            nameof(MaterializedViewsCache));

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
}