using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {
        static Devmasters.Cache.Elastic.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, Firma>
           _VZCache = Devmasters.Cache.Elastic.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, Firma>
               .GetSafeInstance("Firma_VZStatistics",
                    (firma) => CalculateVZStat(firma),
                    TimeSpan.Zero,
                    Devmasters.Config.GetWebConfigValue("ESConnection").Split(';'),
                    Devmasters.Config.GetWebConfigValue("ElasticCacheDbname"),
                    keyValueSelector: f => f.ICO);


        static Devmasters.Cache.Elastic.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, (Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost)>
           _holdingVZCache = Devmasters.Cache.Elastic.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, (Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost)>
               .GetSafeInstance("Holding_VZStatistics",
                   (obj) => CalculateHoldingVZStat(obj.firma, obj.aktualnost),
                   TimeSpan.Zero,
Devmasters.Config.GetWebConfigValue("ESConnection").Split(';'),
Devmasters.Config.GetWebConfigValue("ElasticCacheDbname"),
keyValueSelector: obj => obj.firma.ICO + "-" + obj.aktualnost.ToString());

        public static StatisticsSubjectPerYear<Firma.Statistics.VZ> CachedHoldingStatisticsVZ(
    Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost,
    bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _holdingVZCache.Delete((firma, aktualnost));

            return _holdingVZCache.Get((firma, aktualnost));
        }

        public static StatisticsSubjectPerYear<Firma.Statistics.VZ> CachedStatisticsVZ(
            Firma firma,
            bool forceUpdateCache = false)
        {

            if (forceUpdateCache)
                _VZCache.Delete(firma);

            return _VZCache.Get(firma);
        }

        private static StatisticsSubjectPerYear<Firma.Statistics.VZ> CalculateHoldingVZStat(Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost)
        {
            var firmy = firma.Holding(aktualnost).ToArray();

            var statistiky = firmy.Select(f => f.StatistikaVerejneZakazky()).Append(firma.StatistikaVerejneZakazky()).ToArray();

            var aggregate = Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.VZ>.Aggregate(statistiky);

            return aggregate;

        }
        public static async Task<StatisticsPerYear<SimpleStat>> VZPerYearAsync(string query, int[] interestedInYearsOnly)
        {
            AggregationContainerDescriptor<VerejnaZakazka> aggYSum =
                new AggregationContainerDescriptor<VerejnaZakazka>()
                    .DateHistogram("x-agg", h => h
                        .Field(f => f.PosledniZmena)
                        .CalendarInterval(DateInterval.Year)
                        .Aggregations(agg => agg
                            .Sum("sumincome", s => s
                                .Field(ff => ff.KonecnaHodnotaBezDPH)
                            )
                        )
                    );

            var res = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(query, new string[] { }, 1, 0,
                "0", exactNumOfResults: true, anyAggregation: aggYSum);


            Dictionary<int, SimpleStat> result = new Dictionary<int, SimpleStat>();
            if (interestedInYearsOnly != null)
            {
                foreach (int year in interestedInYearsOnly)
                {
                    result.Add(year, new SimpleStat());
                }

                foreach (DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["x-agg"]).Items)
                {
                    if (result.ContainsKey(val.Date.Year))
                    {
                        result[val.Date.Year].Pocet = val.DocCount ?? 0;
                        result[val.Date.Year].CelkemCena =
                            (decimal)(((DateHistogramBucket)val).Sum("sumincome").Value ?? 0);
                    }
                }
            }
            else
            {
                foreach (DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["x-agg"]).Items)
                {
                    result.Add(val.Date.Year, new SimpleStat());
                    result[val.Date.Year].Pocet = val.DocCount ?? 0;
                    result[val.Date.Year].CelkemCena =
                        (decimal)(((DateHistogramBucket)val).Sum("sumincome").Value ?? 0);
                }
            }

            return new StatisticsPerYear<SimpleStat>(result);
        }
        private static StatisticsSubjectPerYear<Firma.Statistics.VZ> CalculateVZStat(Firma f)
        {
            StatisticsPerYear<SimpleStat> resVZdodav = VZPerYearAsync("icododavatel:" + f.ICO, HlidacStatu.Lib.Analytics.Consts.VZYearsList)
                .ConfigureAwait(false).GetAwaiter().GetResult();
            StatisticsPerYear<SimpleStat> resVZzadav = VZPerYearAsync("icozadavatel:" + f.ICO, HlidacStatu.Lib.Analytics.Consts.VZYearsList)
                .ConfigureAwait(false).GetAwaiter().GetResult();


            Dictionary<int, Firma.Statistics.VZ> data = new Dictionary<int, Firma.Statistics.VZ>();
            foreach (var year in Consts.VZYearsList)
            {
                var stat = new Firma.Statistics.VZ();

                if (resVZdodav.Any(m => m.Year == year))
                {
                    stat.PocetJakoDodavatel = resVZdodav.FirstOrDefault(m => m.Year == year).Value.Pocet;
                    stat.CelkovaHodnotaJakoDodavatel = resVZdodav.FirstOrDefault(m => m.Year == year).Value.CelkemCena;
                }
                if (resVZzadav.Any(m => m.Year == year))
                {
                    stat.PocetJakoZadavatel = resVZzadav.FirstOrDefault(m => m.Year == year).Value.Pocet;
                    stat.CelkovaHodnotaJakoZadavatel = resVZzadav.FirstOrDefault(m => m.Year == year).Value.CelkemCena;
                }
                data.Add(year, stat);

            }
            return new StatisticsSubjectPerYear<Firma.Statistics.VZ>(f.ICO, data);
        }

    }
}
