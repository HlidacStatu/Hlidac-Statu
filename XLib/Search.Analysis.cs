using HlidacStatu.Entities;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.Analysis.KorupcniRiziko;
using HlidacStatu.Repositories.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.XLib
{
    public partial class Search
    {

        public class FullAnalysis
        {
            public class KidxRiziko
            {
                public string Name { get; set; }
                public KIndexData.KIndexParts KIndexPart { get; set; }
                public Entities.Analysis.Riziko.RizikoValues Value { get; set; }    
                public string Query { get; set; }
            }

            public HlidacStatu.Lib.Analytics.StatisticsPerYear<Smlouva.Statistics.Data> Statistics { get; set; }
            public List<StatisticsSubjectPerYear<SimpleStat>> TopDodavatele { get; set; }
            public IEnumerable<(string ico, SimpleStat stat)> TopDodavateleCurrSeason { get; set; }
            public List<StatisticsSubjectPerYear<SimpleStat>> TopZadavatele { get; set; }
            public IEnumerable<(string ico, SimpleStat stat)> TopZadavateleCurrSeason { get; set; }
            public Repositories.Searching.SmlouvaSearchResult Smlouvy { get; set; }
            public int CurrentSeasonYear { get; set; }
            public int[] Years { get; set; }
            public List<KidxRiziko> Rizika { get; set; } = new();
        }
        public static async Task<FullAnalysis> AnalysisAsync(string query)
        {

            int[] years = HlidacStatu.Lib.Analytics.Consts.RegistrSmluvYearsList.ToArray(); //.Where(m => m > 2018).ToArray();


            var t1 = SmlouvyStatistics.CalculateAsync(query);
            var t2 = HlidacStatu.Repositories.ES.QueryGrouped.TopDodavatelePerYearStatsAsync(query, years);
            var t3 = HlidacStatu.Repositories.ES.QueryGrouped.TopOdberatelePerYearStatsAsync(query, years);
            var t4 = SmlouvaRepo.Searching.SimpleSearchAsync(query, 1, 1, 0,
                            anyAggregation: new Nest.AggregationContainerDescriptor<Smlouva>().Sum("sumKc",
                                m => m.Field(f => f.CalculatedPriceWithVATinCZK))
                        );



            await Task.WhenAll(t1, t2, t3, t4);

            Repositories.Searching.SmlouvaSearchResult smlouvy = t4.Result;

            HlidacStatu.Lib.Analytics.StatisticsPerYear<Smlouva.Statistics.Data> statistics =
                new StatisticsPerYear<Smlouva.Statistics.Data>(t1.Result);
            List<(int Year, Smlouva.Statistics.Data Value)> statisticsAfter2016 = statistics
                .Where(s => statistics.YearsAfter2016().Contains(s.Year))
                .OrderBy(s => s.Year).ToList();


            int currentSeasonYear = statistics.CurrentSeasonYear();

            Repositories.ES.QueryGrouped.ResultCombined topDodavateleFull = t2.Result;

            List<StatisticsSubjectPerYear<SimpleStat>> topDodavatele = topDodavateleFull.PerIco.CombinedTop(50);
            IEnumerable<(string ico, SimpleStat stat)> topDodavateleCurrSeason = topDodavateleFull.PerYear.CombinedTop(currentSeasonYear, 10);

            Repositories.ES.QueryGrouped.ResultCombined topZadavateleFull = t3.Result;

            List<StatisticsSubjectPerYear<SimpleStat>> topZadavatele = topZadavateleFull.PerIco.CombinedTop(50);
            IEnumerable<(string ico, SimpleStat stat)> topZadavateleCurrSeason = topZadavateleFull.PerYear.CombinedTop(currentSeasonYear, 10);


            var res = new FullAnalysis()
            {
                CurrentSeasonYear = currentSeasonYear,
                Statistics = statistics,
                Smlouvy = smlouvy,
                Years = years,

                TopDodavatele = topDodavatele,
                TopDodavateleCurrSeason = topDodavateleCurrSeason,

                TopZadavatele = topZadavatele,
                TopZadavateleCurrSeason = topZadavateleCurrSeason,
            };


            //rizika
            var statSum = statistics.Summary(statistics.YearsAfter2016());
            if (statSum.PercentSmluvBezCeny > 0)
            {
                res.Rizika.Add(
                    new FullAnalysis.KidxRiziko()
                    {
                        Name = "Smlouvy bez uvedené ceny",
                        KIndexPart = KIndexData.KIndexParts.PercentBezCeny,
                        Value = KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercentBezCeny, statSum.PercentSmluvBezCeny).AsRiziko(),
                        Query = System.Net.WebUtility.UrlEncode($"( {query} ) AND ( hint.skrytaCena:1 ) ")
                    }
                    );
            }
            if (statSum.PocetSmluvULimitu > 0)
            {
                res.Rizika.Add(
                    new FullAnalysis.KidxRiziko()
                    {
                        Name = "Smlouvy těsně pod limitem pro veřejné zakázky",
                        KIndexPart = KIndexData.KIndexParts.PercSmluvUlimitu,
                        Value = KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercSmluvUlimitu, statSum.PercentSmluvULimitu).AsRiziko(),
                        Query = System.Net.WebUtility.UrlEncode($"( {query} ) AND ( hint.smlouvaULimitu:>0 ) ")
                    }
                    );
            }

            if (statSum.PocetSmluvSeZasadnimNedostatkem > 0)
            {
                res.Rizika.Add(
                    new FullAnalysis.KidxRiziko()
                    {
                        Name = "Smlouvy těsně pod limitem pro veřejné zakázky",
                        KIndexPart = KIndexData.KIndexParts.PercSeZasadnimNedostatkem,
                        Value = KIndexData.DetailInfo.KIndexLabelForPart(KIndexData.KIndexParts.PercSeZasadnimNedostatkem, statSum.PercentSmluvSeZasadnimNedostatkem).AsRiziko(),
                        Query = System.Net.WebUtility.UrlEncode($"( {query} ) AND ( chyby:zasadni ) ")
                    }
                    );
            }

            return res;
        }

    }
}
