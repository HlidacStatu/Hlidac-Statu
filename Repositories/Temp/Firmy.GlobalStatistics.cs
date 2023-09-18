using Devmasters.Batch;

using HlidacStatu.Connectors;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Manager = Devmasters.Batch.Manager;


namespace HlidacStatu.Repositories
{
    public static partial class Firmy
    {
        //Předtěhovat do tasku?
        public class GlobalStatistics
        {

            static List<string> _vsechnyUrady = null;
            private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1, 1);
            
            public static async Task<List<string>> VsechnyUradyAsync(
                Action<string> logOutputFunc = null,
                Action<ActionProgressData> progressOutputFunc = null,
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

                        Util.Consts.Logger.Info($"Loading ALL ICOS");

                        icos.AddRange(FirmaRepo.KrajskeUradyCache.Get().Select(m => m.ICO));
                        icos.AddRange(FirmaRepo.MinisterstvaCache.Get().Select(m => m.ICO));
                        // krajske technicke sluzby
                        icos.AddRange(
                            "00066001,00090450,70947023,70946078,72053119,00080837,70971641,27502988,00085031,70932581,70960399,00095711,26913453,03447286,25396544,60733098"
                                .Split(','));
                        //velke nemocnice
                        icos.AddRange(
                            "00064165,00064173,00064203,00098892,00159816,00179906,00669806,00843989,25488627,26365804,27283933,27661989,65269705,27283518,26000202,00023736,00023884,27256391,61383082,27256537,00023001,27520536,26068877,47813750,00064211,00209805,27660915,00635162,27256456,00090638,00092584,00064190"
                                .Split(','));
                        //fakultni nemocnice
                        icos.AddRange(
                            "00064165,00064173,00064203,00098892,00159816,00179906,00669806,00843989,25488627,26365804,27283933,27661989,65269705,27283518,26000202,00023736,00023884,27256391,61383082,27256537,00023001,27520536,26068877,47813750,00064211,00209805,27660915,00635162,27256456,00090638,00092584,00064190"
                                .Split(','));

                        //CEZ, CPost, CD, 
                        icos.AddRange("45274649,47114983,70994226".Split(','));
                        icos.AddRange(Firma.StatniFirmyICO);

                        icos.AddRange(DirectDB
                            .GetList<string>(
                                "select distinct ico from firma where status =1 and Kod_PF in (301,302,312,313,314,325,331,352,353,361,362,381,382,521,771,801,804,805)")
                        );

                        icos.AddRange(FirmaRepo.ObceSRozsirenouPusobnostiCache.Get().Select(m => m.ICO));
                        //velka mesta
                        string velkamesta =
                            "00064581,00081531,00266094,00254657,00262978,44992785,00845451,00274046,00075370,00262978,00299308,00244732,00283924";
                        icos.AddRange(velkamesta.Split(','));

                        icos.AddRange(
                            FirmaRepo.Zatrideni.Subjekty(Firma.Zatrideni.SubjektyObory.Vse).Select(m => m.Ico));

                        //nejvice utajujici smluvni strany
                        Util.Consts.Logger.Info($"Loading ICOS utajujici smluvni strany");
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
                            var f = Get(ico);
                            if (f.PatrimStatu())
                            {
                                if (f.StatistikaRegistruSmluv().Any(m =>
                                        m.Value.PocetSmluv >= Entities.Entities.KIndex.Consts.MinSmluvPerYear)
                                   )
                                    icos.Add(ico);
                            }
                            else
                            {
                            }
                        }

                        //nejvice utajujici ceny
                        Util.Consts.Logger.Info($"Loading ICOS utajujici ceny");
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
                            var f = Get(ico);
                            if (f.PatrimStatu())
                            {
                                if (f.StatistikaRegistruSmluv().Any(m =>
                                        m.Value.PocetSmluv >= Entities.Entities.KIndex.Consts.MinSmluvPerYear)
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

                        Util.Consts.Logger.Info("Dohledani podrizenych organizaci");
                        //podrizene organizace
                        var allIcos = new System.Collections.Concurrent.ConcurrentBag<string>();

                        Manager.DoActionForAll<string>(icos.Distinct().ToArray(),
                            (i) =>
                            {
                                var fk = Get(i);
                                allIcos.Add(i);
                                foreach (var pic in fk.IcosInHolding(Relation.AktualnostType.Aktualni))
                                    allIcos.Add(pic);
                                
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

            private static
                Dictionary<int,
                Devmasters.Cache.Elastic.Cache<GlobalStatisticsPerYear<Smlouva.Statistics.Data>>>
                _uradySmlouvyGlobal =
                    new();


            public static GlobalStatisticsPerYear<Smlouva.Statistics.Data> UradySmlouvyGlobal(int? obor = null,
                GlobalStatisticsPerYear<Smlouva.Statistics.Data> newData = null)
            {
                obor = obor ?? 0;

                if (!_uradySmlouvyGlobal.ContainsKey(obor.Value))
                    _uradySmlouvyGlobal[obor.Value] = new Devmasters.Cache.Elastic.Cache<GlobalStatisticsPerYear<Smlouva.Statistics.Data>>(
                            Devmasters.Config.GetWebConfigValue("ESConnection").Split(';'),
                            "DevmastersCache",
                            TimeSpan.Zero, $"UradySmlouvyGlobal_{obor.Value}",
                            o =>
                            {
                                //fill from Lib.Data.AnalysisCalculation.CalculateGlobalRankPerYear_UradySmlouvy && Tasks.UpdateWebCache
                                return null;
                            }, providerId: "HlidacStatu.Lib"
                            );

                if (newData != null)
                {
                    _uradySmlouvyGlobal[obor.Value].ForceRefreshCache(newData);
                    return newData;
                }
                else
                    return _uradySmlouvyGlobal[obor.Value].Get();

            }

            public static async Task CalculateGlobalRangeCaches_UradySmlouvyAsync(int? threads = null,
         Action<string> logOutputFunc = null, Action<ActionProgressData> progressOutputFunc = null)
            {
                UradySmlouvyGlobal(null, await CalculateGlobalRankPerYear_UradySmlouvyAsync(null, threads, logOutputFunc, progressOutputFunc));
                foreach (var main in Devmasters.Enums.EnumTools
                    .EnumToEnumerable(typeof(Smlouva.SClassification.ClassificationsTypes))
                    .Select(m => new { value = Convert.ToInt32(m.Id), key = m.Name })
                    .Where(m => m.value % 100 == 0)
                    )
                {
                    UradySmlouvyGlobal(main.value, await CalculateGlobalRankPerYear_UradySmlouvyAsync(null, threads, logOutputFunc, progressOutputFunc));
                }
            }

            private static async Task<GlobalStatisticsPerYear<Smlouva.Statistics.Data>> CalculateGlobalRankPerYear_UradySmlouvyAsync(
                 int? obor = null,
                 int? threads = null,
                 Action<string> logOutputFunc = null, Action<ActionProgressData> progressOutputFunc = null
                 )
            {
                obor = obor ?? 0;
                var icos = await VsechnyUradyAsync(logOutputFunc, progressOutputFunc);
                object lockObj = new object();
                List<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> data =
                    new List<StatisticsSubjectPerYear<Smlouva.Statistics.Data>>();

                Manager.DoActionForAll<string>(icos,
                    (Func<string, ActionOutputData>)(
                    ico =>
                    {
                        var stat = Get(ico)?.StatistikaRegistruSmluv(obor.Value);
                        if (stat != null)
                            lock (lockObj)
                            {
                                data.Add(stat);
                            }
                        return new ActionOutputData();
                    }), logOutputFunc, progressOutputFunc, true, maxDegreeOfParallelism: threads
                    ,prefix: $"CalculateGlobalRankPerYear_UradySmlouvy_{obor.Value} "
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
