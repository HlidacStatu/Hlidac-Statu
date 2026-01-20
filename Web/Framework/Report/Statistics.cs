using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.XLib.Render;
using Nest;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Framework.Report
{
    public static class GlobalStatistics
    {
        public static async Task<ReportDataSource> PocetSmluvPerUzavreniAsync(string query, Nest.DateInterval interval)
        {
            DateTime minDate = new DateTime(2012, 1, 1);
            DateTime maxDate = DateTime.Now.Date.AddDays(1);
            string datumFormat = "MMM yyyy";
            switch (interval)
            {
                case DateInterval.Day:
                    datumFormat = "dd.MM.yy";
                    break;
                case DateInterval.Week:
                    datumFormat = "dd.MM.yy";
                    break;
                case DateInterval.Month:
                    datumFormat = "MMM yyyy";
                    break;
                case DateInterval.Quarter:
                    datumFormat = "MMM yyyy";
                    break;
                case DateInterval.Year:
                    datumFormat = "yyyy";
                    break;
                default:
                    break;
            }

            AggregationContainerDescriptor<Smlouva> aggs = new AggregationContainerDescriptor<Smlouva>()
                .DateHistogram("x-agg", h => h
                    .Field(f => f.datumUzavreni)
                    .CalendarInterval(interval)
                );

            var res = await SmlouvaRepo.Searching.SimpleSearchAsync(
                "( " + query + " ) AND datumUzavreni:{" + HlidacStatu.Util.RenderData.ToElasticDate(minDate) + " TO " +
                HlidacStatu.Util.RenderData.ToElasticDate(maxDate) + "}", 1, 0,
                SmlouvaRepo.Searching.OrderResult.FastestForScroll, anyAggregation: aggs, exactNumOfResults: true);

            ReportDataSource rds = new(new ReportDataSource.Column[]
                {
                    new()
                    {
                        Name = "Datum",
                        TextRender = (s) => { return ((DateTime)s).ToString(datumFormat); },
                        ValueRender = (s) =>
                        {
                            DateTime dt = ((DateTime)s).ToUniversalTime();
                            return string.Format("Date.UTC({0}, {1}, {2})", dt.Year, dt.Month, dt.Day);
                        },
                        OrderValueRender = (s) => { return ((DateTime)s).Ticks.ToString(); }
                    },
                    new()
                    {
                        Name = "Počet smluv",
                        ValueRender = (s) => { return ((long?)s).Value.ToString("F0", Util.Consts.enCulture); },
                        OrderValueRender = (s) => { return HlidacStatu.Util.RenderData.OrderValueFormat((double?)s); },
                    },
                }
            );

            foreach (Nest.DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["x-agg"]).Items)
            {
                if (val.Date >= minDate && val.Date <= maxDate)
                    rds.AddRow(
                        val.Date,
                        val.DocCount
                    );
            }


            return rds;
        }

        public static async Task<ReportDataSource> HodnotaSmluvPerUzavreniAsync(string query, DateInterval interval,
            DateTime? minDate = null, DateTime? maxDate = null)
        {
            minDate = minDate ?? new DateTime(2012, 1, 1);
            maxDate = maxDate ?? DateTime.Now.Date.AddDays(1);

            AggregationContainerDescriptor<Smlouva> aggs = new AggregationContainerDescriptor<Smlouva>()
                .DateHistogram("x-agg", h => h
                    .Field(f => f.datumUzavreni)
                    .CalendarInterval(interval)
                    .Format("yyyy-MM-dd")
                    .Aggregations(agg => agg
                        .Sum("sumincome", s => s
                            .Field(ff => ff.CalculatedPriceWithVATinCZK)
                        )
                    )
                );
            ReportDataSource rdsPerIntervalSumPrice = new(new ReportDataSource.Column[]
            {
                new()
                {
                    Name = "Měsíc",
                    TextRender = (s) =>
                    {
                        DateTime dt = ((DateTime)s).ToUniversalTime();
                        return string.Format("Date.UTC({0}, {1}, {2})", dt.Year, dt.Month, dt.Day);
                    },
                    ValueRender = (s) =>
                    {
                        DateTime dt = ((DateTime)s).ToUniversalTime();
                        return string.Format("Date.UTC({0}, {1}, {2})", dt.Year, dt.Month, dt.Day);
                    },
                    OrderValueRender = (s) =>
                    {
                        DateTime dt = ((DateTime)s).ToUniversalTime();
                        return HlidacStatu.Util.RenderData.OrderValueFormat(dt);
                    },
                },
                new()
                {
                    Name = "Součet cen",
                    HtmlRender =(s) => { return Smlouva.NicePrice((double?)s, html: true, shortFormat: true); },
                    ValueRender = (s) => { return ((double?)s).Value.ToString("F0", Util.Consts.enCulture); },
                    OrderValueRender = (s) => { return HlidacStatu.Util.RenderData.OrderValueFormat((double?)s); },
                },
            });

            var res = await SmlouvaRepo.Searching.SimpleSearchAsync(
                "( " + query + " ) AND datumUzavreni:{" + HlidacStatu.Util.RenderData.ToElasticDate(minDate.Value) +
                " TO " + HlidacStatu.Util.RenderData.ToElasticDate(maxDate.Value) + "}", 1, 0,
                SmlouvaRepo.Searching.OrderResult.FastestForScroll, anyAggregation: aggs, exactNumOfResults: true);

            foreach (Nest.DateHistogramBucket val in
                     ((BucketAggregate)res.ElasticResults.Aggregations["x-agg"]).Items
                    )
            {
                if (val.Date >= minDate && val.Date <= maxDate)
                    rdsPerIntervalSumPrice.AddRow(
                        new DateTime(val.Date.Ticks, DateTimeKind.Utc).ToLocalTime(),
                        ((Nest.DateHistogramBucket)val).Sum("sumincome").Value
                    );
            }


            return rdsPerIntervalSumPrice;
        }

        public static Task<ReportDataSource> PocetSmluvPerUzavreniAsync(Nest.DateInterval interval)
        {
            return PocetSmluvPerUzavreniAsync("-id:pre* ", interval);
        }

        public static Task<ReportDataSource> HodnotaSmluvPerUzavreniAsync(DateInterval interval)
        {
            return HodnotaSmluvPerUzavreniAsync("-id:pre* ", interval);
        }


        public static async Task<ReportDataSource> TopListPerCountAsync(bool platce)
        {
            string field = "prijemce.ico";
            if (platce)
                field = "platce.ico";

            int size = 300;
            AggregationContainerDescriptor<Smlouva> aggs = new AggregationContainerDescriptor<Smlouva>()
                .Terms("perIco", m => m
                    .Field(field)
                    .Size(size)
                );

            var res = await SmlouvaRepo.Searching.RawSearchAsync("{\"query_string\": { \"query\": \"-id:pre*\" } }", 1,
                0, anyAggregation: aggs);

            ReportDataSource rdsPerIco = new(new ReportDataSource.Column[]
                {
                    new()
                    {
                        Name = "IČO",
                        HtmlRender =(s) =>
                        {
                            System.Tuple<string, string> data = (System.Tuple<string, string>)s;
                            return string.Format("<a href='/subjekt/{0}'>{1}</a>", data?.Item2,
                                data?.Item1?.Replace("&", "&amp;"));
                        },
                        ValueRender = (s) =>
                        {
                            System.Tuple<string, string> data = (System.Tuple<string, string>)s;
                            return string.Format("\"{0}\"", data.Item1.Replace("&", "&amp;"));
                        },
                        TextRender = (s) => { return ((System.Tuple<string, string>)s).Item1.ToString(); }
                    },
                    new()
                    {
                        Name = "Počet smluv",
                        HtmlRender =(s) =>
                        {
                            return HlidacStatu.Util.RenderData.NiceNumber(
                                HlidacStatu.Util.ParseTools.ToDecimal((string)s) ?? 0, html: true);
                        },
                        ValueRender = (s) =>
                        {
                            return HlidacStatu.Util.ParseTools.ToDecimal((string)s)
                                ?.ToString("F0", Util.Consts.enCulture) ?? "0";
                        },
                        OrderValueRender = (s) =>
                        {
                            return HlidacStatu.Util.RenderData.OrderValueFormat(
                                HlidacStatu.Util.ParseTools.ToDecimal((string)s) ?? 0);
                        }
                    },
                }
            );

            foreach (Nest.KeyedBucket<object> val in ((BucketAggregate)res.Aggregations["perIco"]).Items)
            {
                Firma f = await Firmy.GetAsync((string)val.Key);
                if (f != null && (!f.PatrimStatu() || platce))
                {
                    rdsPerIco.AddRow(
                        new Tuple<string, string>(await Firmy.GetJmenoAsync((string)val.Key), (string)val.Key),
                        val.DocCount.ToString()
                    );
                }
            }

            return rdsPerIco;
        }

        public static async Task<ReportDataSource> TopListPerSumAsync(bool platce)
        {
            string field = "prijemce.ico";
            if (platce)
                field = "platce.ico";

            int size = 300;
            AggregationContainerDescriptor<Smlouva> aggs = new AggregationContainerDescriptor<Smlouva>()
                .Terms("perPrice", m => m
                    .Order(o => o.Descending("sumincome"))
                    .Field(field)
                    .Size(size)
                    .Aggregations(agg => agg
                        .Sum("sumincome", s => s
                            .Field(ff => ff.CalculatedPriceWithVATinCZK)
                        )
                    )
                );

            var res = await SmlouvaRepo.Searching.RawSearchAsync("{\"query_string\": { \"query\": \"-id:pre*\" } }", 1,
                0, anyAggregation: aggs);

            ReportDataSource rdsPerPrice = new(new ReportDataSource.Column[]
                {
                    new()
                    {
                        Name = "IČO",
                        HtmlRender =(s) =>
                        {
                            System.Tuple<string, string> data = (System.Tuple<string, string>)s;
                            return string.Format("<a href='/subjekt/{0}'>{1}</a>", data.Item2,
                                data.Item1.Replace("&", "&amp;"));
                        },
                        ValueRender = (s) =>
                        {
                            System.Tuple<string, string> data = (System.Tuple<string, string>)s;
                            return string.Format("\"{0}\"", data.Item1.Replace("&", "&amp;"));
                        },
                        TextRender = (s) => { return ((System.Tuple<string, string>)s).Item1.ToString(); }
                    },
                    new()
                    {
                        Name = "Součet cen",
                        HtmlRender =(s) => { return Smlouva.NicePrice((double?)s, html: true, shortFormat: true); },
                        ValueRender = (s) => { return ((double?)s)?.ToString("F0", Util.Consts.enCulture) ?? "0"; },
                        OrderValueRender = (s) => { return HlidacStatu.Util.RenderData.OrderValueFormat((double?)s); }
                    },
                }
            );
            ;
            ;
            foreach (Nest.KeyedBucket<object> val in ((BucketAggregate)res.Aggregations["perPrice"]).Items)
            {
                Firma f = await Firmy.GetAsync((string)val.Key);
                if (f != null && (!f.PatrimStatu() || platce))
                {
                    rdsPerPrice.AddRow(
                        new Tuple<string, string>(await Firmy.GetJmenoAsync((string)val.Key), (string)val.Key),
                        val.Sum("sumincome").Value
                    );
                }
            }

            return rdsPerPrice;
        }

        public static Task<ReportDataSource> SmlouvyPodleCenyAsync()
        {
            return SmlouvyPodleCenyAsync("-id:pre* ");
        }

        public static async Task<ReportDataSource> SmlouvyPodleCenyAsync(string query)
        {
            DateTime minDate = new DateTime(2012, 1, 1);
            DateTime maxDate = DateTime.Now.Date.AddDays(1);

            double[] ranks = new double[] { 0, 50000, 100000, 500000, 1000000, 5000000, 10000000 };
            AggregationContainerDescriptor<Smlouva> aggs = new AggregationContainerDescriptor<Smlouva>()
                .PercentileRanks("x-agg", h => h
                    .Field(f => f.CalculatedPriceWithVATinCZK)
                    .Values(ranks)
                );

            var res = await SmlouvaRepo.Searching.SimpleSearchAsync(
                "(" + query + ") AND datumUzavreni:{" + HlidacStatu.Util.RenderData.ToElasticDate(minDate) + " TO " +
                HlidacStatu.Util.RenderData.ToElasticDate(maxDate) + "}", 1, 0,
                SmlouvaRepo.Searching.OrderResult.FastestForScroll, anyAggregation: aggs, exactNumOfResults: true);

            ReportDataSource rds = new(new ReportDataSource.Column[]
                {
                    new()
                    {
                        Name = "Hodnota smlouvy",
                        TextRender = (s) => { return s.ToString(); }
                    },
                    new()
                    {
                        Name = "% smluv",
                        TextRender = (s) => { return ((double)s).ToString("N2") + "%"; },
                        ValueRender = (s) => { return ((double)s).ToString("F2") + "%"; },
                        HtmlRender =(s) => { return ((double)s).ToString("N2") + "%"; },
                        OrderValueRender = (s) => { return HlidacStatu.Util.RenderData.OrderValueFormat(((double)s)); }
                    },
                }
            );
            rds.Title = "Smlouvy podle hodnoty";
            var data = ((PercentilesAggregate)res.ElasticResults.Aggregations["x-agg"]).Items.ToArray();
            double prevVal = 0;

            for (int i = 0; i < data.Count(); i++)
            {
                string x = data[i].Percentile.ToString("N0") + " Kč";
                if (i > 0)
                {
                    x = data[i - 1].Percentile.ToString("N0") + " Kč -" + x;
                }
                else
                    x = "Bez ceny";

                rds.AddRow(x, data[i].Value - prevVal);
                prevVal = data[i].Value ?? 0;
            }

            rds.AddRow("nad " + data[data.Count() - 1].Percentile.ToString("N0") + " Kč", 100 - prevVal);
            return rds;
        }
    }
}