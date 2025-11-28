using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hlidacstatu.Caching;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories.Statistics;
using Serilog;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public static class OsobaCache
{
    // private static readonly ILogger _logger = Log.ForContext(typeof(FirmaCache));

    private static readonly IFusionCache PostgreCache =
        Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2PostgreSql, nameof(OsobaCache));

    // private static readonly IFusionCache MemcachedCache =
    //     Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L2Memcache, nameof(OsobaCache));

    public static ValueTask<InfoFact[]> GetInfoFactsAsync(Osoba osoba) =>
        PostgreCache.GetOrSetAsync($"_InfoFacts:{osoba.NameId}",
            _ => OsobaExtension.InfoFactsAsync(osoba)
        );

    public static ValueTask InvalidateInfoFactsAsync(Osoba osoba) =>
        PostgreCache.ExpireAsync($"_InfoFacts:{osoba.NameId}");


    // new shiit

    //todo: Imho tohle by se mělo rozhodit po osobě a ne dělat všechny
    public static ValueTask<Dictionary<int, Osoba.Statistics.VerySimple[]>> GetTopPoliticiObchodujiciSeStatemAsync() =>
        PostgreCache.GetOrSetAsync($"_TopPoliticiObchodujiciSeStatem", async _ =>
            {
                List<Osoba.Statistics.RegistrSmluv> allStats = new();
                await Devmasters.Batch.Manager.DoActionForAllAsync(OsobaRepo.Politici.Get(), async (o) =>
                    {
                        if (o != null)
                        {
                            var stat = await o.StatistikaRegistrSmluvAsync();
                            if (stat.SmlouvyStat_SoukromeFirmySummary().HasStatistics &&
                                stat.SmlouvyStat_SoukromeFirmySummary().Summary().PocetSmluv > 0)
                            {
                                allStats.Add(stat);
                            }
                        }

                        return new Devmasters.Batch.ActionOutputData();
                    },
                    Util.Consts.outputWriter.OutputWriter,
                    Util.Consts.progressWriter.ProgressWriter,
                    true, //!System.Diagnostics.Debugger.IsAttached,
                    maxDegreeOfParallelism: 6, prefix: "TopPoliticiObchodSeStatem loading ",
                    monitor: new MonitoredTaskRepo.ForBatch());
                
                Dictionary<int, Osoba.Statistics.VerySimple[]> res = new();
                res.Add(0,
                    allStats.OrderByDescending(o =>
                            o.SmlouvyStat_SoukromeFirmySummary().Summary().CelkovaHodnotaSmluv).Take(100)
                        .Union(allStats.OrderByDescending(o =>
                            o.SmlouvyStat_SoukromeFirmySummary().Summary().PocetSmluv).Take(100))
                        .Select(m => new Osoba.Statistics.VerySimple()
                            {
                                OsobaNameId = m.OsobaNameId,
                                CelkovaHodnotaSmluv = m.SmlouvyStat_SoukromeFirmySummary().Summary()
                                    .CelkovaHodnotaSmluv,
                                PocetSmluv = m.SmlouvyStat_SoukromeFirmySummary().Summary().PocetSmluv,
                                Year = 0,
                            }
                        )
                        .ToArray()
                );
                foreach (int year in HlidacStatu.Entities.KIndex.Consts.ToCalculationYears)
                {
                    res.Add(year,
                        allStats.OrderByDescending(o =>
                                o.SmlouvyStat_SoukromeFirmySummary()[year].CelkovaHodnotaSmluv).Take(100)
                            .Union(allStats.OrderByDescending(o =>
                                o.SmlouvyStat_SoukromeFirmySummary()[year].PocetSmluv).Take(100))
                            .Select(m => new Osoba.Statistics.VerySimple()
                                {
                                    OsobaNameId = m.OsobaNameId,
                                    CelkovaHodnotaSmluv = m.SmlouvyStat_SoukromeFirmySummary()[year]
                                        .CelkovaHodnotaSmluv,
                                    PocetSmluv = m.SmlouvyStat_SoukromeFirmySummary()[year].PocetSmluv,
                                    Year = year,
                                }
                            )
                            .ToArray()
                    );
                }

                return res;
            }, options =>
            {
                options.Duration = TimeSpan.FromDays(7);
                options.FailSafeMaxDuration = TimeSpan.FromHours(14);
                options.DistributedCacheFailSafeMaxDuration = TimeSpan.FromDays(30);
            }
        );
    
    public static ValueTask InvalidateITopPoliticiObchodujiciSeStatemAsync() =>
        PostgreCache.ExpireAsync($"_TopPoliticiObchodujiciSeStatem");

    //todo: Imho tohle by se mělo rozhodit po osobě a ne dělat všechny
    public static ValueTask<Tuple<Osoba.Statistics.RegistrSmluv, Entities.Insolvence.RizeniStatistic[]>[]>
        GetPoliticiSFirmouVInsolvenciAsync() =>
        PostgreCache.GetOrSetAsync($"_PoliticiSFirmouVInsolvenci", async _ =>
            {
                var ret = new ConcurrentBag<
                    Tuple<Osoba.Statistics.RegistrSmluv, Entities.Insolvence.RizeniStatistic[]>>();

                await Devmasters.Batch.Manager.DoActionForAllAsync<Osoba>(
                    OsobaRepo.PolitickyAktivni.Get().Where(m => m.StatusOsoby() == Osoba.StatusOsobyEnum.Politik)
                        .Distinct(),
                    async (o) =>
                    {
                        var icos = o.AktualniVazby(Relation.AktualnostType.Nedavny)
                            .Where(w => !string.IsNullOrEmpty(w.To.Id))
                            .Select(w => w.To.Id)
                            .ToList();

                        if (icos.Any())
                        {
                            var res = await InsolvenceRepo.Searching.SimpleSearchAsync(
                                "osobaiddluznik:" + o.NameId, 1, 100,
                                (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult
                                    .LatestUpdateDesc,
                                limitedView: false);
                            if (res.IsValid && res.Total > 0)
                            {
                                var insolvenceIntoList = new List<Entities.Insolvence.Rizeni>();
                                foreach (var i in res.ElasticResults.Hits.Select(m => m.Source))
                                {
                                    bool addToList = false;
                                    var pdluznici = i.Dluznici.Where(m => icos.Contains(m.ICO));

                                    foreach (var pd in pdluznici)
                                    {
                                        var vazby = o.VazbyProICO(pd.ICO);
                                        foreach (var v in vazby)
                                        {
                                            if (Devmasters.DT.Util.IsOverlappingIntervals(
                                                    i.DatumZalozeni, i.PosledniZmena, v.RelFrom, v.RelTo))
                                            {
                                                addToList = true;
                                                break;
                                            }
                                        }

                                        if (addToList)
                                            break;
                                    }

                                    if (addToList)
                                        insolvenceIntoList.Add(i);
                                }

                                if (insolvenceIntoList.Any())
                                {
                                    var stat = await o.StatistikaRegistrSmluvAsync();

                                    ret.Add(
                                        new Tuple<Osoba.Statistics.RegistrSmluv,
                                            Entities.Insolvence.RizeniStatistic[]>(
                                            stat, insolvenceIntoList
                                                .Select(m =>
                                                    new Entities.Insolvence.RizeniStatistic(m, icos))
                                                .ToArray()
                                        )
                                    );
                                }
                            }
                        }

                        return new Devmasters.Batch.ActionOutputData();
                    },
                    Util.Consts.outputWriter.OutputWriter,
                    Util.Consts.progressWriter.ProgressWriter,
                    true, //!System.Diagnostics.Debugger.IsAttached,
                    maxDegreeOfParallelism: 6, prefix: "Insolvence politiku ",
                    monitor: new MonitoredTaskRepo.ForBatch());

                return ret.ToArray();
            }, options =>
            {
                options.Duration = TimeSpan.FromDays(7);
                options.FailSafeMaxDuration = TimeSpan.FromHours(14);
                options.DistributedCacheFailSafeMaxDuration = TimeSpan.FromDays(30);
            }
        );
    
    public static ValueTask InvalidatePoliticiSFirmouVInsolvenciAsync() =>
        PostgreCache.ExpireAsync($"_PoliticiSFirmouVInsolvenci");
}