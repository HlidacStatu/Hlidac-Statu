using Devmasters.Batch;
using HlidacStatu.Connectors;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using Nest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Cache;
using Serilog;
using Manager = Devmasters.Batch.Manager;


namespace HlidacStatu.Repositories
{
    public static partial class Firmy
    {
        public const string Krajske_Technicke_Sluzby = "00066001,00090450,70947023,70946078,72053119,00080837,70971641,27502988,00085031,70932581,70960399,00095711,26913453,03447286,25396544,60733098";
        public const string Velke_Nemocnice = "00064165,00064173,00064203,00098892,00159816,00179906,00669806,00843989,25488627,26365804,27283933,27661989,65269705,27283518,26000202,00023736,00023884,27256391,61383082,27256537,00023001,27520536,26068877,47813750,00064211,00209805,27660915,00635162,27256456,00090638,00092584,00064190";
        public const string VelkaMesta = "00064581,00081531,00266094,00254657,00262978,44992785,00845451,00274046,00075370,00262978,00299308,00244732,00283924";

        //Předtěhovat do tasku?
        public class GlobalStatistics
        {
            private static readonly ILogger _logger = Log.ForContext(typeof(GlobalStatistics));

            static List<string> _vsechnyUrady = null;
            private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);

            public static async Task<List<string>> VsechnyUradyAsync(
                Action<string> logOutputFunc = null,
                IProgressWriter progressOutputFunc = null,
                int? threads = null)
            {
                if (_vsechnyUrady != null)
                    return _vsechnyUrady;
                else
                {
                    await _semaphoreSlim.WaitAsync();
                    try
                    {
                        if (_vsechnyUrady != null)
                            return _vsechnyUrady;

                        var icos = new List<string>();

                        _logger.Information($"Loading ALL ICOS");

                        icos.AddRange(FirmaCache.KrajskeUradyAsync().Select(m => m.ICO));
                        icos.AddRange(FirmaCache.MinisterstvaAsync().Select(m => m.ICO));
                        // krajske technicke sluzby
                        icos.AddRange(
                            Krajske_Technicke_Sluzby.Split(','));
                        //velke nemocnice
                        icos.AddRange(
                            Velke_Nemocnice.Split(','));
                        //fakultni nemocnice
                        icos.AddRange(
                            Velke_Nemocnice
                                .Split(','));

                        //CEZ, CPost, CD, 
                        icos.AddRange("45274649,47114983,70994226".Split(','));
                        icos.AddRange(FirmaVlastnenaStatemRepo.StatniFirmyICO);

                        icos.AddRange(DirectDB.Instance
                            .GetList<string>(
                                "select distinct ico from firma where status =1 and Kod_PF in (301,302,312,313,314,325,331,352,353,361,362,381,382,521,771,801,804,805)")
                        );

                        icos.AddRange(FirmaCache.ObceSRozsirenouPusobnostiAsync().Select(m => m.ICO));
                        //velka mesta
                        string velkamesta =
                            VelkaMesta;
                        icos.AddRange(velkamesta.Split(','));

                        icos.AddRange(
                            (await FirmaCache.GetSubjektyForOborAsync(Firma.Zatrideni.SubjektyObory.Vse)).Select(m =>
                                m.Ico));

                        //nejvice utajujici smluvni strany
                        _logger.Information($"Loading ICOS utajujici smluvni strany");
                        AggregationContainerDescriptor<Smlouva> aggs = new AggregationContainerDescriptor<Smlouva>()
                            .Terms("perIco", m => m
                                .Field("platce.ico")
                                .Size(2500)
                                .Order(o => o.Descending("_count"))
                            );

                        var res = await SmlouvaRepo.Searching.SimpleSearchAsync(
                            "(issues.issueTypeId:18 OR issues.issueTypeId:12)", 1, 0,
                            SmlouvaRepo.Searching.OrderResult.FastestForScroll, aggs, platnyZaznam: true);

                        foreach (KeyedBucket<object> val in ((BucketAggregate)res.ElasticResults.Aggregations["perIco"])
                                 .Items)
                        {
                            var ico = (string)val.Key;
                            var f = await GetAsync(ico);
                            if (f?.PatrimStatu() == true)
                            {
                                if ((await f.StatistikaRegistruSmluvAsync()).Any(m =>
                                        m.Value.PocetSmluv >= Entities.KIndex.Consts.MinPocetSmluvPerYear)
                                   )
                                    icos.Add(ico);
                            }
                            else
                            {
                            }
                        }

                        //nejvice utajujici ceny
                        _logger.Information($"Loading ICOS utajujici ceny");
                        aggs = new AggregationContainerDescriptor<Smlouva>()
                            .Terms("perIco", m => m
                                .Field("platce.ico")
                                .Size(2500)
                                .Order(o => o.Descending("_count"))
                            );

                        res = await SmlouvaRepo.Searching.SimpleSearchAsync("(hint.skrytaCena:1)", 1, 0,
                            SmlouvaRepo.Searching.OrderResult.FastestForScroll, aggs, platnyZaznam: true);

                        foreach (KeyedBucket<object> val in ((BucketAggregate)res.ElasticResults.Aggregations["perIco"])
                                 .Items)
                        {
                            var ico = (string)val.Key;
                            var f = await GetAsync(ico);
                            if (f?.PatrimStatu() == true)
                            {
                                if ((await f.StatistikaRegistruSmluvAsync()).Any(m =>
                                        m.Value.PocetSmluv >= Entities.KIndex.Consts.MinPocetSmluvPerYear)
                                   )
                                    icos.Add(ico);
                            }
                            else
                            {
                                if (System.Diagnostics.Debugger.IsAttached)
                                {
                                    //System.Diagnostics.Debugger.Break();
                                    Console.WriteLine("excl:" + f.Jmeno);
                                }
                            }
                        }

                        _logger.Information("Dohledani podrizenych organizaci");
                        //podrizene organizace
                        var allIcos = new System.Collections.Concurrent.ConcurrentBag<string>();

                        await Manager.DoActionForAllAsync<string>(icos.Distinct().ToArray(), async (i) =>
                            {
                                var fk = await Firmy.GetAsync(i);
                                if (fk != null)
                                {
                                    allIcos.Add(i);
                                    foreach (var pic in await fk.IcosInHoldingAsync(Relation.AktualnostType.Aktualni))
                                        allIcos.Add(pic);
                                }
                                return new ActionOutputData();
                            },
                            logOutputFunc,
                            progressOutputFunc,
                            true, maxDegreeOfParallelism: threads, prefix: "VsechnyUrady ");


                        icos = allIcos.ToList();
                        _vsechnyUrady = icos
                            .Select(i => Util.ParseTools.NormalizeIco(i))
                            .Distinct()
                            .ToList();
                    }
                    finally
                    {
                        _semaphoreSlim.Release();
                    }
                }

                return _vsechnyUrady;
            }


            public static async Task CalculateGlobalRangeCaches_UradySmlouvyAsync(int? threads = null,
                Action<string> logOutputFunc = null, IProgressWriter progressOutputFunc = null)
            {
                await StatisticsCache.UradySmlouvyGlobalAsync(null,
                    await CalculateGlobalRankPerYear_UradySmlouvyAsync(null, threads, logOutputFunc,
                        progressOutputFunc));
                foreach (var main in Devmasters.Enums.EnumTools
                             .EnumToEnumerable(typeof(Smlouva.SClassification.ClassificationsTypes))
                             .Select(m => new { value = Convert.ToInt32(m.Id), key = m.Name })
                             .Where(m => m.value % 100 == 0)
                        )
                {
                    await StatisticsCache.UradySmlouvyGlobalAsync(main.value,
                        await CalculateGlobalRankPerYear_UradySmlouvyAsync(null, threads, logOutputFunc,
                            progressOutputFunc));
                }
            }

            public static async Task<GlobalStatisticsPerYear<Smlouva.Statistics.Data>>
                CalculateGlobalRankPerYear_UradySmlouvyAsync(
                    int? obor = null,
                    int? threads = null,
                    Action<string> logOutputFunc = null, IProgressWriter progressOutputFunc = null
                )
            {
                obor = obor ?? 0;
                var icos = await VsechnyUradyAsync(logOutputFunc, progressOutputFunc);
                ConcurrentBag<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> data =
                    new ConcurrentBag<StatisticsSubjectPerYear<Smlouva.Statistics.Data>>();

                await Manager.DoActionForAllAsync<string>(icos,
                    (Func<string, Task<ActionOutputData>>)(async ico =>
                    {
                        var f = await Firmy.GetAsync(ico);
                        if (f != null)
                        {
                            var stat = await f.StatistikaRegistruSmluvAsync(obor.Value);
                            if (stat != null)
                                data.Add(stat);
                        }
                        return new ActionOutputData();
                    }), logOutputFunc, progressOutputFunc, true, maxDegreeOfParallelism: threads
                    , prefix: $"CalculateGlobalRankPerYear_UradySmlouvy_{obor.Value} "
                    , monitor: progressOutputFunc != null ? new MonitoredTaskRepo.ForBatch() : null
                );

                return new GlobalStatisticsPerYear<Smlouva.Statistics.Data>(
                    Consts.RegistrSmluvYearsList,
                    data,
                    m => m.PocetSmluv >= 10
                );
            }
        }
    }
}