using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
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
        //L1 - 1 h
        //L2 - 10 let

        static Devmasters.Cache.Redis.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, Firma>
           _VZCache = Devmasters.Cache.Redis.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, Firma>
               .GetSafeInstance("Firma_VZStatistics_",
                    (firma) => CalculateVZStat(firma),
                    TimeSpan.Zero,
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                    keyValueSelector: f => f.ICO);

        //L1 - 1 h
        //L2 - 10 let

        static Devmasters.Cache.Redis.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, Firma>
           _holdingVZCache = Devmasters.Cache.Redis.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, Firma>
               .GetSafeInstance("Holding_VZStatistics_",
                   (f) => CalculateHoldingVZStat(f),
                    TimeSpan.Zero,
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                    keyValueSelector: f => f.ICO );

        public static StatisticsSubjectPerYear<Firma.Statistics.VZ> CachedHoldingStatisticsVZ(
            Firma firma, 
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache )
                _holdingVZCache.Delete(firma);

                return _holdingVZCache.Get(firma);
        }

        public static void RemoveStatisticsVZ(Firma firma)
        {
            _VZCache.Delete(firma);
            _holdingVZCache.Delete(firma);
        }
        public static StatisticsSubjectPerYear<Firma.Statistics.VZ> CachedStatisticsVZ(
        Firma firma,
        bool forceUpdateCache = false)
        {

            if (forceUpdateCache )
                _VZCache.Delete(firma);

                return _VZCache.Get(firma);
        }

        private static StatisticsSubjectPerYear<Firma.Statistics.VZ> CalculateHoldingVZStat(Firma f)
        {

            var firmy_maxrok = new Dictionary<string, Devmasters.DT.DateInterval>();
            firmy_maxrok.Add(f.ICO, new Devmasters.DT.DateInterval(DateTime.MinValue, DateTime.MaxValue));
            var skutecneVazby = Relation.SkutecnaDobaVazby(f.AktualniVazby(DS.Graphs.Relation.AktualnostType.Libovolny));
            foreach (var v in skutecneVazby)
            {
                if (!string.IsNullOrEmpty(v.To?.UniqId)
                            && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                {
                    firmy_maxrok.TryAdd(v.To.Id, new Devmasters.DT.DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
                }

            }
            var statistiky = firmy_maxrok
                .Select(f => new StatisticsSubjectPerYear<Firma.Statistics.VZ>(
                                f.Key,
                                Firmy.Get(f.Key).StatistikaVerejneZakazky().Filter(m => m.Key >= f.Value.From?.Year && m.Key <= f.Value.To?.Year)
                           )
                )
                .ToArray();
            if (statistiky.Length == 0)
                return new StatisticsSubjectPerYear<Firma.Statistics.VZ>(f.ICO);
            if (statistiky.Length == 1)
                return statistiky[0];

            StatisticsSubjectPerYear<Firma.Statistics.VZ> aggregate =
                Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.VZ>.Aggregate(f.ICO, statistiky);

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
