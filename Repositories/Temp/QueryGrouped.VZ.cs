using HlidacStatu.Entities.VZ;
using HlidacStatu.Entities.Entities.Analysis;

using Nest;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Searching;

namespace HlidacStatu.Repositories.ES
{
    public partial class QueryGrouped
    {
        public class VZ
        {

            public static async Task<Dictionary<int, BasicData>> KonecnaCenaPerYearAsync(string query, int[] interestedInYearsOnly)
            {
                AggregationContainerDescriptor<VerejnaZakazka> aggYSum =
                    new AggregationContainerDescriptor<VerejnaZakazka>()
                        .DateHistogram("x-agg", h => h
                            .Field(f => f.DatumUverejneni)
                            .CalendarInterval(DateInterval.Year)
                            .Aggregations(agg => agg
                                .Sum("sumincome", s => s
                                    .Field(ff => ff.KonecnaHodnotaBezDPH)
                                )
                            )
                        );

                var q = new VerejnaZakazkaSearchData()
                {
                    Q = query,
                    OrigQuery = query,
                    Page = 1,
                    PageSize = 0,
                    Order = "666",
                    ExactNumOfResults = false
                };

                var res = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(q, aggYSum);

                Dictionary<int, BasicData> result = new Dictionary<int, BasicData>();
                if (interestedInYearsOnly != null)
                {
                    foreach (int year in interestedInYearsOnly)
                    {
                        result.Add(year, BasicData.Empty());
                    }

                    foreach (DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["x-agg"]).Items)
                    {
                        if (result.ContainsKey(val.Date.Year))
                        {
                            result[val.Date.Year].Pocet = val.DocCount ?? 0;
                            result[val.Date.Year].CelkemCena = (decimal)(((DateHistogramBucket)val).Sum("sumincome").Value ?? 0);
                        }
                    }
                }
                else
                {
                    foreach (DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["x-agg"]).Items)
                    {
                        result.Add(val.Date.Year, BasicData.Empty());
                        result[val.Date.Year].Pocet = val.DocCount ?? 0;
                        result[val.Date.Year].CelkemCena = (decimal)(((DateHistogramBucket)val).Sum("sumincome").Value ?? 0);
                    }

                }

                return result;
            }
            
            //todo: Tahle metoda skrývá metodu se stejným názvem z vnější classy
            public static Task<Dictionary<int, (List<(string ico, BasicData stat)> topPodlePoctu, List<(string ico, BasicData stat)> topPodleKc)>> TopDodavatelePerYearAsync(
                string query,
                int[] interestedInYearsOnly,
                int maxList = 50
                ) => _topStranyPerYearAsync("dodavatele.iCO", query, interestedInYearsOnly, maxList);


            public static Task<Dictionary<int, (List<(string ico, BasicData stat)> topPodlePoctu, List<(string ico, BasicData stat)> topPodleKc)>> TopZadavatelePerYearAsync(
                string query,
                int[] interestedInYearsOnly,
                int maxList = 50
                ) => _topStranyPerYearAsync("zadavatel.iCO", query, interestedInYearsOnly, maxList);

            private static async Task<Dictionary<int, (List<(string ico, BasicData stat)> topPodlePoctu, List<(string ico, BasicData stat)> topPodleKc)>> _topStranyPerYearAsync(
                    string property,
                    string query,
                    int[] interestedInYearsOnly,
                    int maxList
                    )
            {
                AggregationContainerDescriptor<VerejnaZakazka> aggs = new AggregationContainerDescriptor<VerejnaZakazka>();
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
                               .Field(ff => ff.KonecnaHodnotaBezDPH)
                           )
                        )
                    );

                Func<AggregationContainerDescriptor<VerejnaZakazka>, AggregationContainerDescriptor<VerejnaZakazka>> aggrFunc
                    = (aggr) => { return aggs; };

                AggregationContainerDescriptor<VerejnaZakazka> aggYSum =
                    new AggregationContainerDescriptor<VerejnaZakazka>()
                        .DateHistogram("y-agg", h => h
                            .Field(f => f.DatumUverejneni)
                            .CalendarInterval(DateInterval.Year)
                            .Aggregations(aggrFunc)
                        );

                var q = new VerejnaZakazkaSearchData()
                {
                    Q = query,
                    OrigQuery = query,
                    Page = 1,
                    PageSize = 0,
                    Order = "666",
                    ExactNumOfResults = false
                };

                var res = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(q, aggYSum);


                Dictionary<int, (List<(string ico, BasicData stat)> topPodlePoctu, List<(string ico, BasicData stat)> topPodleKc)> result =
                    new Dictionary<int, (List<(string ico, BasicData stat)> topPodlePoctu, List<(string ico, BasicData stat)> topPodleKc)>();

                if (interestedInYearsOnly != null)
                    foreach (int year in interestedInYearsOnly)
                    {
                        result.Add(year,
                            (new List<(string ico, BasicData stat)>(),
                            new List<(string ico, BasicData stat)>())
                            );
                    }

                foreach (DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["y-agg"]).Items)
                {
                    if (interestedInYearsOnly == null)
                    {
                        result.Add(val.Date.Year,
                            (new List<(string ico, BasicData stat)>(),
                            new List<(string ico, BasicData stat)>())
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
                                .OrderByDescending(m => m.cc)
                                .Select(m => (m.ico, new BasicData()
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
                                .Select(m => (m.ico, new BasicData()
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

        }
    }
}