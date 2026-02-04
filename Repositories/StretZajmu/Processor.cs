using HlidacStatu.Caching;
using HlidacStatu.Entities;
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


    public class Processor
    {

        private static IFusionCache PermanentCache =>
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore, nameof(Processor));


        public async static Task<List<OsobaStret>> Paragraf_4_Async(bool forceUpdate = false)
        {
            const string key = "Paragraf_4";

            if (forceUpdate)
            {
                await PermanentCache.ExpireAsync(key);
            }


            var res = await PermanentCache.GetOrSetAsync(key,
                async _ => await _generate_paragraf_4_Async(),
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromDays(10 * 365), TimeSpan.FromDays(10 * 365))
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
            var o1_c = await Data.Vlada_1c_Async();
            //var o1_d = await Data.Namestci_1d_Async();

            List<OsobaStret> res = new();

            // a)

            foreach (var os in o1_c.Osoby)
            {
                OsobaStret st = new OsobaStret()
                {
                    ParagrafOdstavec = "§ 4 odst. 1 písm. c)",
                    Osoba = os.Osoba
                };


                var efektivni_doba_stretu_osoby = Devmasters.DT.DateInterval.GetOverlappingInterval(
                    new Devmasters.DT.DateInterval(os.Event.DatumOd, os.Event.DatumDo),
                    o1_c.PravniInterval
                    );
                if (efektivni_doba_stretu_osoby == null)
                    continue;

                st.Stret_za_obdobi = efektivni_doba_stretu_osoby;

                DS.Graphs.Graph.Edge[] vazby = await os.Osoba.AktualniVazbyAsync(
                    DS.Graphs.Relation.CharakterVazbyEnum.VlastnictviKontrola,
                    DS.Graphs.Relation.AktualnostType.Libovolny);




                var aktivniVazby_dobe_platnosti = vazby
                    .Where(v => v.DateInterval().IsOverlappingIntervalsWith(o1_c.PravniInterval))
                    .DistinctBy(v => v.To.Id)
                    .ToArray();

                int count = 0;

                await Devmasters.Batch.Manager.DoActionForAllAsync(aktivniVazby_dobe_platnosti,
                    async (ed) =>
                    {

                        var efektivni_doba_stretu = Devmasters.DT.DateInterval.GetOverlappingInterval(efektivni_doba_stretu_osoby, o1_c.PravniInterval);
                        if (efektivni_doba_stretu == null)
                            return new Devmasters.Batch.ActionOutputData();


                        var firma = await HlidacStatu.Repositories.Cache.FirmaCache.GetAsync(ed.To.Id);
                        if (firma != null)
                        {
                            if (!
                                (firma.TypSubjektu == Firma.TypSubjektuEnum.Soukromy 
                                || firma.TypSubjektu == Firma.TypSubjektuEnum.Neznamy
                                || firma.TypSubjektu == Firma.TypSubjektuEnum.InsolvecniSpravce
                                || firma.TypSubjektu == Firma.TypSubjektuEnum.Exekutor
                                )
                            )
                                return new Devmasters.Batch.ActionOutputData();


                            var smlouvyStatTask = SmlouvyStatAsync(firma, efektivni_doba_stretu);
                            var dotaceStatTask = DotaceStatAsync(firma, efektivni_doba_stretu);

                            await Task.WhenAll(smlouvyStatTask, dotaceStatTask);

                            StretFirma sz = new StretFirma()
                            {
                                VazbaDistance = ed.Distance,
                                Firma = firma,
                                Vazba = ed,
                                Za_obdobi = efektivni_doba_stretu,
                                SmlouvyStat = await smlouvyStatTask,
                                DotaceStat = await dotaceStatTask,
                            };

                            //if (sz.SmlouvyStat.Summary().PocetSmluv>0 || sz.DotaceStat.Summary().PocetDotaci > 0) {
                            st.Strety.Add(sz);
                        }
                        return new Devmasters.Batch.ActionOutputData();
                    },
                        null,
                        new Devmasters.Batch.ActionProgressWriter(0.1f, Devmasters.Batch.ProgressWriters.ConsoleWriter_EndsIn),
                        true,
                        10, prefix: $" Paragraf_4_Async pro {os.Osoba.FullName()} "
                        , monitor: new MonitoredTaskRepo.ForBatchAsync()
                    );

                if (st.Strety.Count > 0)
                    res.Add(st);
            }

            return res;
        }

        public async static Task<StatisticsSubjectPerYear<Smlouva.Statistics.Data>>
            SmlouvyStatAsync(Firma f, Devmasters.DT.DateInterval interval)
        {
            string iOd = interval.From?.ToString("yyyy-MM-dd") ?? "*";
            string iDo = interval.To?.ToString("yyyy-MM-dd") ?? "*";

            var query = $"ico:{f.ICO} AND podepsano:[{iOd} TO {iDo}] ";

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
            int yTo = interval.To?.Year ?? DateTime.Now.Year+1;
            int[] years = Enumerable.Range(yFrom, yTo - yFrom + 1).ToArray();

            StatisticsSubjectPerYear<Firma.Statistics.Dotace> res = await HlidacStatu.Repositories
                .Cache.StatisticsCache.GetFirmaDotaceStatisticsAsync(f);

            Dictionary<int, Firma.Statistics.Dotace> filteredyears = res.Filter(y => years.Contains(y.Key));

            return new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(f.ICO, filteredyears);
        }
    }
}
