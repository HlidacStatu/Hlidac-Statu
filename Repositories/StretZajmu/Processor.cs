using Devmasters.DT;
using Force.DeepCloner;
using HlidacStatu.Caching;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories.ProfilZadavatelu;
using HlidacStatu.Repositories.Statistics;
using Json.More;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.StretZajmu
{

    /*
     
    var o_1e = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };

    //Mgr. Petr Jäger, Ph.D. (*1980).
    var o_1f = new Role() { ZakonDatumLide = new DateTime(2025, 1, 1) };

    //Ing. Jiří Kratochvíl 12. 3. 1981
    var o_1g = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };

    /*
     "Ing. Mgr. Jaromír Novák","2012– "
"Ing. Ondřej Filip, MBA","2013– "
"Ing. Jan Duben","2014– "
"Ing. Josef Bednář","2014– "
"RNDr. Ing. Jiří Peterka","2015–2025"
"Mgr. Josef Chomyn","2017– "
"Mgr. Lukáš Zelený","2019– "
"Ing. Mgr. Hana Továrková","2019–2023"
"Ing. Marek Ebert","2020– "
"Ing. Jiří Šuchman","2022– "
"JUDr. Marek Vrbík","2024– "
"Mgr. Vlastimil Turza","2025– "
     
    var o_1h = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };


    var o_1i = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };
    var o_1j = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };
    var o_1k = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };
    var o_1l = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };
    var o_1m = new Role() { ZakonDatumLide = new DateTime(2025, 7, 1) };

    //masakr
    var o_2b = new Role() { ZakonDatumLide = new DateTime(2022, 7, 1) };
    var o_2c = new Role() { ZakonDatumLide = new DateTime(2009, 10, 9) };
    var o_2d = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };
    var o_2e = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };
    var o_2f = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };
    var o_2g = new Role() { ZakonDatumLide = new DateTime(2017, 9, 1) };

     */

    public class Processor
    {

        private static IFusionCache PermanentCache =>
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore, nameof(Processor));


        public async static Task<List<OsobaStret>> Paragraf_4_Async(bool forceUpdate = false)
        {
            const string key = "Paragraf_4";

            if (forceUpdate)
            {
                await PermanentCache.RemoveAsync(key);
                await Task.Delay(200); // počkejme chvíli, ať se to projeví
            }


            var res = await PermanentCache.GetOrSetAsync(key,
                async _ => await _generate_paragraf_4_Async(),
                options => options.ModifyEntryOptionsDuration(
#if DEBUG
                    TimeSpan.FromSeconds(1),
#else
                    TimeSpan.FromHours(1),
#endif
                    TimeSpan.FromDays(10 * 365))
            );
            return res;
        }

        /*
         * Veřejný funkcionář uvedený v § 2 odst. 1 písm. c) až m) nesmí
01.01.2007 a) podnikat nebo provozovat jinou samostatnou výdělečnou činnost,
01.09.2017 b) být členem statutárního orgánu, členem řídícího, dozorčího nebo kontrolního orgánu právnické osoby, která podniká (dále jen „podnikající právnická osoba“), pokud zvláštní právní předpis nestanoví jinak, nebo
01.01.2007 c) být v pracovněprávním nebo obdobném vztahu nebo ve služebním poměru, nejde-li o vztah nebo poměr, v němž působí jako veřejný funkcionář.

         */
        private async static Task<List<OsobaStret>> _generate_paragraf_4_Async()
        {
            /*
01.09.2017  (1) Veřejný funkcionář uvedený v § 2 odst. 1 písm. c) až m) nesmí
01.01.2007      a) podnikat nebo provozovat jinou samostatnou výdělečnou činnost,
01.09.2017      b) být členem statutárního orgánu, členem řídícího, dozorčího nebo kontrolního orgánu právnické osoby, která podniká (dále jen „podnikající právnická osoba“), pokud zvláštní právní předpis nestanoví jinak, nebo
01.01.2007      c) být v pracovněprávním nebo obdobném vztahu nebo ve služebním poměru, nejde-li o vztah nebo poměr, v němž působí jako veřejný funkcionář.
01.01.2007  (2) Omezení podle odstavce 1 se nevztahuje na správu vlastního majetku a na činnost vědeckou, pedagogickou, publicistickou, literární, uměleckou nebo sportovní, nejde-li o vlastní podnikání v těchto oborech.
            
            */



            var o1_c = await Data.Vlada_1c_Async();
            o1_c.ZakonDatumParagraf = new DateTime(2017, 2, 9);
            //var o1_d = await Data.Namestci_1d_Async();

            List<OsobaStret> res = new();

            // a)

            foreach (var os in o1_c.Osoby)
            {

                var efektivni_doba_stretu_osoby = Devmasters.DT.DateInterval.GetOverlappingInterval(
                    new Devmasters.DT.DateInterval(os.Event.DatumOd, os.Event.DatumDo),
                    o1_c.PravniInterval
                    );
                if (efektivni_doba_stretu_osoby == null)
                    continue;


                OsobaStret st = new OsobaStret()
                {
                    ParagrafOdstavec = "§ 4 odst. 1 písm. c)",
                    Osoba_with_Event = os,
                    Stret_za_obdobi = efektivni_doba_stretu_osoby,
                };


                
                DS.Graphs.Graph.Edge[] vazby = await os.Osoba.AktualniVazbyAsync(
                    DS.Graphs.Relation.CharakterVazbyEnum.VlastnictviKontrola,
                    DS.Graphs.Relation.AktualnostType.Libovolny,true);




                var aktivniVazby_dobe_platnosti = vazby
                    .Where(v => v.DateInterval().IsOverlappingIntervalsWith(efektivni_doba_stretu_osoby))
                    .DistinctBy(v => v.To.Id)
                    .ToArray();

                var debugVazby = aktivniVazby_dobe_platnosti.Where(v => v.To.Id == "46973460").ToArray();

                int count = 0;

                bool debug = true;
                await Devmasters.Batch.Manager.DoActionForAllAsync(aktivniVazby_dobe_platnosti,
                    async (vazba_edge) =>
                    {

                        var efektivni_doba_stretu_s_firmou = Devmasters.DT.DateInterval.GetOverlappingInterval(efektivni_doba_stretu_osoby, vazba_edge.DateInterval());
                        if (efektivni_doba_stretu_s_firmou == null)
                            return new Devmasters.Batch.ActionOutputData();
                        var firma = await HlidacStatu.Repositories.Cache.FirmaCache.GetAsync(vazba_edge.To.Id);

                        if (_spadaPodStredZajmu(firma) == false)
                        {
                            return new Devmasters.Batch.ActionOutputData();
                        }

                        if (vazba_edge.Distance > 1)
                        { //je to dcerinka, najdi vazby zpet k matce

                            //dohledat vsechny vazby k firme 
                            var cesty = await PlneVazby.Core.AllPathsAsync(os.Osoba,
                                DS.Graphs.Relation.CharakterVazbyEnum.VlastnictviKontrola,
                                firma.ICO,
                                efektivni_doba_stretu_s_firmou);


                            if (cesty.Any())
                            {                            
                                //merge vyhledanych časových úseků
                                var casoveuseky = DateInterval.MergeDateIntervals(cesty.Select(p => p.ValidInterval).ToArray());

                                foreach (var c_usek in casoveuseky)
                                {

                                    var smlouvyStatTask = SmlouvyStatAsync(firma, c_usek);
                                    var dotaceStatTask = DotaceStatAsync(firma, c_usek);

                                    await Task.WhenAll(smlouvyStatTask, dotaceStatTask);

                                    StretFirma sz = new StretFirma()
                                    {
                                        VazbaDistance = vazba_edge.Distance,
                                        Firma = firma,
                                        Vazba = vazba_edge,
                                        Za_obdobi = c_usek,
                                        SmlouvyStat = await smlouvyStatTask,
                                        DotaceStat = await dotaceStatTask,
                                    };

                                    //if (sz.SmlouvyStat.Summary().PocetSmluv>0 || sz.DotaceStat.Summary().PocetDotaci > 0) {
                                    st.Strety.Add(sz);
                                }
                            }
                        }
                        return new Devmasters.Batch.ActionOutputData();
                    },
                        null,
                        new Devmasters.Batch.ActionProgressWriter(0.1f, Devmasters.Batch.ProgressWriters.ConsoleWriter_EndsIn),
                        !debug,
                        10, prefix: $" Paragraf_4_Async pro {os.Osoba.FullName()} "
                        , monitor: new MonitoredTaskRepo.ForBatchAsync()
                    );

                if (st.Strety.Count > 0)
                    res.Add(st);
            }

            return res;
        }

        private static bool _spadaPodStredZajmu(Firma firma)
        {
            if (firma == null)
                return false;

            if (firma.PatrimStatu())
                return false;
            if (firma.JsemOVM())
                return false;
            if (firma.JsemNeziskovka())
                return false;
            if (firma.JsemStatniFirma())
                return false;
            if (firma.JsemPolitickaStrana())
                return false;
            return true;
        }

        public async static Task<StatisticsSubjectPerYear<Smlouva.Statistics.Data>>
            SmlouvyStatAsync(Firma f, Devmasters.DT.DateInterval interval)
        {
            string iOd = interval.From?.ToString("yyyy-MM-dd") ?? "*";
            string iDo = interval.To?.ToString("yyyy-MM-dd") ?? "*";

            var query = $"ico:{f.ICO} AND podepsano:[{iOd} TO {iDo} }} ";

            StatisticsSubjectPerYear<Smlouva.Statistics.Data> res = null;
            res = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                f.ICO,
                await SmlouvyStatistics.CalculateAsync(query)
            );
            return res;
        }


        public async static Task<StatisticsSubjectPerYear<Firma.Statistics.Dotace>> DotaceStatAsync(Firma f,
            Devmasters.DT.DateInterval interval)
        {
            int yFrom = interval.From?.Year ?? 1990;
            int yTo = interval.To?.Year ?? DateTime.Now.Year + 1;
            int[] years = Enumerable.Range(yFrom, yTo - yFrom + 1).ToArray();

            StatisticsSubjectPerYear<Firma.Statistics.Dotace> res = await HlidacStatu.Repositories
                .Cache.StatisticsCache.GetFirmaDotaceStatisticsAsync(f);

            Dictionary<int, Firma.Statistics.Dotace> filteredyears = res.Filter(y => years.Contains(y.Key));

            return new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(f.ICO, filteredyears);
        }
    }
}
