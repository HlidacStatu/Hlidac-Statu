using HlidacStatu.Entities;
using HlidacStatu.Lib.Analytics;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.ES
{
    public partial class QueryGrouped
    {
        public static async Task<Dictionary<int, Dictionary<int, SimpleStat>>> OblastiPerYearAsync(string query,
            int[] interestedInYearsOnly)
        {
            AggregationContainerDescriptor<Smlouva> aggYSum =
                new AggregationContainerDescriptor<Smlouva>()
                    .DateHistogram("x-agg", h => h
                        .Field(f => f.datumUzavreni)
                        .CalendarInterval(DateInterval.Year)
                        .Aggregations(aggObor => aggObor
                            .Terms("x-obor", oborT => oborT
                                .Field("classification.class1.typeValue")
                                .Size(150)
                                .Aggregations(agg => agg
                                    .Sum("sumincome", s => s
                                        .Field(ff => ff.CalculatedPriceWithVATinCZK)
                                    )
                                )
                            )
                        )
                    );

            var res = await SmlouvaRepo.Searching.SimpleSearchAsync(query, 1, 0,
                SmlouvaRepo.Searching.OrderResult.FastestForScroll, aggYSum, exactNumOfResults: true);


            Dictionary<int, Dictionary<int, SimpleStat>> result = new Dictionary<int, Dictionary<int, SimpleStat>>();
            if (interestedInYearsOnly != null)
            {
                foreach (int year in interestedInYearsOnly)
                {
                    result.Add(year, new Dictionary<int, SimpleStat>());
                }

                foreach (DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["x-agg"]).Items)
                {
                    if (result.ContainsKey(val.Date.Year))
                    {
                        BucketAggregate vals = (BucketAggregate)val.Values.FirstOrDefault();
                        var oblasti = vals.Items.Select(m =>
                            new
                            {
                                oblast = Convert.ToInt32(((KeyedBucket<object>)m).Key),
                                data = new SimpleStat()
                                {
                                    CelkemCena =
                                        (decimal)((ValueAggregate)((KeyedBucket<object>)m).Values.FirstOrDefault())
                                        .Value,
                                    Pocet = ((KeyedBucket<object>)m).DocCount ?? 0
                                }
                            }
                        ).ToArray();

                        result[val.Date.Year] = oblasti.ToDictionary(k => k.oblast, v => v.data);
                    }
                }
            }
            else
            {
                foreach (DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["x-agg"]).Items)
                {
                    if (result.ContainsKey(val.Date.Year))
                    {
                        BucketAggregate vals = (BucketAggregate)val.Values.FirstOrDefault();
                        var oblasti = vals.Items.Select(m =>
                            new
                            {
                                oblast = Convert.ToInt32(((KeyedBucket<object>)m).Key),
                                data = new SimpleStat()
                                {
                                    CelkemCena =
                                        (decimal)((ValueAggregate)((KeyedBucket<object>)m).Values.FirstOrDefault())
                                        .Value,
                                    Pocet = ((KeyedBucket<object>)m).DocCount ?? 0
                                }
                            }
                        ).ToArray();
                        result.Add(val.Date.Year, new Dictionary<int, SimpleStat>());
                        result[val.Date.Year] = oblasti.ToDictionary(k => k.oblast, v => v.data);
                    }
                }
            }

            return result;
        }

        public static async Task<StatisticsPerYear<SimpleStat>> SmlouvyPerYearAsync(string query, int[] interestedInYearsOnly)
        {
            AggregationContainerDescriptor<Smlouva> aggYSum =
                new AggregationContainerDescriptor<Smlouva>()
                    .DateHistogram("x-agg", h => h
                        .Field(f => f.datumUzavreni)
                        .CalendarInterval(DateInterval.Year)
                        .Aggregations(agg => agg
                            .Sum("sumincome", s => s
                                .Field(ff => ff.CalculatedPriceWithVATinCZK)
                            )
                        )
                    );

            var res = await SmlouvaRepo.Searching.SimpleSearchAsync(query, 1, 0,
                SmlouvaRepo.Searching.OrderResult.FastestForScroll, aggYSum, exactNumOfResults: true);


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

        public static async Task<Dictionary<int, (List<(string ico, SimpleStat stat)> topPodlePoctu, List<(string ico, SimpleStat stat)> topPodleKc)>> 
            TopOdberatelePerYearAsync(
                string query,
                int[] interestedInYearsOnly,
                int maxList = 50
            ) => await _topSmluvniStranyPerYearAsync("platce.ico", query, interestedInYearsOnly, maxList);


        public static async Task<Dictionary<int, (List<(string ico, SimpleStat stat)> topPodlePoctu, List<(string ico, SimpleStat stat)> topPodleKc)>> 
            TopDodavatelePerYearAsync(
                string query,
                int[] interestedInYearsOnly,
                int maxList = 50
            ) => await _topSmluvniStranyPerYearAsync("prijemce.ico", query, interestedInYearsOnly, maxList);

        public static async Task<ResultCombined> TopDodavatelePerYearStatsAsync(
                string query,
                int[] interestedInYearsOnly,
                int maxList = 9000
            ) => await _topSmluvniStranyPerYearStatsAsync("prijemce.ico", query, interestedInYearsOnly, maxList);

        public static async Task<ResultCombined> TopOdberatelePerYearStatsAsync(
            string query,
            int[] interestedInYearsOnly,
            int maxList = 9000
            ) => await _topSmluvniStranyPerYearStatsAsync("platce.ico", query, interestedInYearsOnly, maxList);

        private static async Task<Dictionary<int, (List<(string ico, SimpleStat stat)> topPodlePoctu, List<(string ico, SimpleStat stat)> topPodleKc)>> 
            _topSmluvniStranyPerYearAsync(
                string property,
                string query,
                int[] interestedInYearsOnly,
                int maxList = 9000
            )
        {
            AggregationContainerDescriptor<Smlouva> aggs = new AggregationContainerDescriptor<Smlouva>();
            aggs
                .Terms("perIco", m => m
                    .Field(property)
                    .Size(maxList)
                ).Terms("perPrice", m => m
                    .Order(o => o.Descending("sumincome"))
                    .Field(property)
                    .Size(maxList)
                    .Aggregations(agg => agg
                        .Sum("sumincome", s => s
                            .Field(ff => ff.CalculatedPriceWithVATinCZK)
                        )
                    )
                );

            Func<AggregationContainerDescriptor<Smlouva>, AggregationContainerDescriptor<Smlouva>> aggrFunc
                = (aggr) => { return aggs; };

            AggregationContainerDescriptor<Smlouva> aggYSum =
                new AggregationContainerDescriptor<Smlouva>()
                    .DateHistogram("y-agg", h => h
                        .Field(f => f.datumUzavreni)
                        .CalendarInterval(DateInterval.Year)
                        .Aggregations(aggrFunc)
                    );


            var res = await SmlouvaRepo.Searching.SimpleSearchAsync(query, 1, 0,
                SmlouvaRepo.Searching.OrderResult.FastestForScroll, aggYSum, exactNumOfResults: true);

            if (!res.IsValid)
                return null;

            Dictionary<int, (List<(string ico, SimpleStat stat)> topPodlePoctu, List<(string ico, SimpleStat stat)>
                topPodleKc)> result =
                new Dictionary<int, (List<(string ico, SimpleStat stat)> topPodlePoctu,
                    List<(string ico, SimpleStat stat)> topPodleKc)>();

            if (interestedInYearsOnly != null)
                foreach (int year in interestedInYearsOnly)
                {
                    result.Add(year,
                        (new List<(string ico, SimpleStat stat)>(),
                            new List<(string ico, SimpleStat stat)>())
                    );
                }

            foreach (DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["y-agg"]).Items)
            {
                if (interestedInYearsOnly == null)
                {
                    result.Add(val.Date.Year,
                        (new List<(string ico, SimpleStat stat)>(),
                            new List<(string ico, SimpleStat stat)>())
                    );
                }

                if (result.ContainsKey(val.Date.Year))
                {
                    result[val.Date.Year] =
                        (topPodlePoctu: ((BucketAggregate)val["perIco"]).Items
                            .Select(m => ((KeyedBucket<object>)m))
                            .Select(m => new
                            {
                                ico = m.Key.ToString(),
                                p = m.DocCount ?? 0,
                                cc = ((Nest.ValueAggregate)m.Values.FirstOrDefault())?.Value ?? 0
                            })
                            .OrderByDescending(m => m.p)
                            .Select(m => (m.ico, new SimpleStat()
                            {
                                Pocet = m.p,
                                CelkemCena = (decimal)m.cc
                            }))
                            .ToList(),
                            topPodleKc: ((BucketAggregate)val["perPrice"]).Items
                            .Select(m => ((KeyedBucket<object>)m))
                            .Select(m => new
                            {
                                ico = m.Key.ToString(),
                                p = m.DocCount ?? 0,
                                cc = ((Nest.ValueAggregate)m.Values.FirstOrDefault())?.Value ?? 0
                            })
                            .OrderByDescending(m => m.cc)
                            .Select(m => (m.ico, new SimpleStat()
                            {
                                Pocet = m.p,
                                CelkemCena = (decimal)m.cc
                            }))
                            .ToList()
                        );
                }
            }

            return result;
        }


        private static async Task<ResultCombined> _topSmluvniStranyPerYearStatsAsync(
                string property,
                string query,
                int[] interestedInYearsOnly,
                int maxList
            )
        {
            var res = new ResultCombined();

            var r1 = await _topSmluvniStranyPerYearAsync(property, query, interestedInYearsOnly, maxList);
            
            if(r1 == null)
                return new ResultCombined();
            
            res.PerYear.TopPodlePoctu = r1.Select(m => new { y = m.Key, v = m.Value.topPodlePoctu })
                .ToDictionary(k => k.y, v => v.v);
            res.PerYear.TopPodleKc = r1.Select(m => new { y = m.Key, v = m.Value.topPodleKc })
                .ToDictionary(k => k.y, v => v.v);

            //convert to 
            List<HlidacStatu.Lib.Analytics.StatisticsSubjectPerYear<SimpleStat>> perIcoStatPodlePoctu
                = new List<StatisticsSubjectPerYear<SimpleStat>>();
            foreach (string ico in r1.Values.SelectMany(m => m.topPodlePoctu.Select(s => s.ico)).Distinct())
            {
                StatisticsSubjectPerYear<SimpleStat> forIco = new StatisticsSubjectPerYear<SimpleStat>();
                forIco.ICO = ico;
                foreach (var y in r1.Keys)
                {
                    if (r1[y].topPodlePoctu.Any(m => m.ico == ico))
                        forIco[y] = r1[y].topPodlePoctu.First(m => m.ico == ico).stat;
                    else
                        forIco[y] = new SimpleStat();
                }

                perIcoStatPodlePoctu.Add(forIco);
            }

            List<HlidacStatu.Lib.Analytics.StatisticsSubjectPerYear<SimpleStat>> perIcoStatPodleKc
                = new List<StatisticsSubjectPerYear<SimpleStat>>();
            foreach (string ico in r1.Values.SelectMany(m => m.topPodleKc.Select(s => s.ico)).Distinct())
            {
                StatisticsSubjectPerYear<SimpleStat> forIco = new StatisticsSubjectPerYear<SimpleStat>();
                forIco.ICO = ico;
                foreach (var y in r1.Keys)
                {
                    if (r1[y].topPodleKc.Any(m => m.ico == ico))
                        forIco[y] = r1[y].topPodleKc.First(m => m.ico == ico).stat;
                    else
                        forIco[y] = new SimpleStat();
                }

                perIcoStatPodleKc.Add(forIco);
            }

            res.PerIco.TopPodlePoctu = perIcoStatPodlePoctu;
            res.PerIco.TopPodleKc = perIcoStatPodleKc;
            return res;
        }

    }
}