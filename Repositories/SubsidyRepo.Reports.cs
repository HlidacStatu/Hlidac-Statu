using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;

namespace HlidacStatu.Repositories
{
    public static partial class SubsidyRepo
    {
        public static readonly int[] DefaultLimitedYears = Enumerable.Range(2010, DateTime.Now.Year - 2010).ToArray();


        public static async Task<List<(int Year, Subsidy.Hint.Type SubsidyType, decimal Sum)>> ReportPoLetechAsync(
            bool limitYears = true, int[] limitedYears = null)
        {
            limitedYears = limitedYears ?? Enumerable.Range(2010, DateTime.Now.Year - 2010).ToArray();

            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perYear", t => t
                    .Field(f => f.ApprovedYear)
                    .Size(10000)
                    .Aggregations(yearAggs => yearAggs
                        .Terms("perSubsidyType", st => st
                            .Field(f => f.Hints.SubsidyType)
                            .Size(10000)
                            .Aggregations(typeAggs => typeAggs
                                .Sum("sumAssumedAmount", sa => sa
                                    .Field(f => f.AssumedAmount)
                                )
                            )
                        )
                    )
                );

            var res = await SubsidyRepo.Searching.SimpleSearchAsync("hints.isOriginal:true", 1, 0, "666",
                anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(int Year, Subsidy.Hint.Type SubsidyType, decimal Sum)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perYear"] is BucketAggregate perYearBA)
            {
                foreach (KeyedBucket<object> perYearBucket in perYearBA.Items)
                {
                    if (int.TryParse(perYearBucket.Key.ToString(), out int year))
                    {
                        if (perYearBucket["perSubsidyType"] is BucketAggregate subsidyTypeBA)
                        {
                            foreach (KeyedBucket<object> subsidyTypeBucket in subsidyTypeBA.Items)
                            {
                                if (Enum.TryParse<Subsidy.Hint.Type>(subsidyTypeBucket.Key.ToString(),
                                        out var subsidyType))
                                {
                                    if (subsidyTypeBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                                    {
                                        if (limitYears == false || (limitYears & limitedYears.Contains(year)))
                                            results.Add((year, subsidyType, Convert.ToDecimal(sumBA.Value ?? 0)));
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        public static async Task<List<(string Ico, int Count, decimal Sum)>> ReportTopPrijemciAsync(int? rok,
            Firma.TypSubjektuEnum? recipientType = null,
            Subsidy.Hint.Type? subsidyType = null)
        {
            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perIco", t => t
                    .Field(f => f.Recipient.Ico)
                    .Size(100)
                    .Aggregations(typeAggs => typeAggs
                        .Sum("sumAssumedAmount", sa => sa
                            .Field(f => f.AssumedAmount)
                        )
                        .ValueCount("docCount", vc => vc
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );

            string query = "hints.isOriginal:true AND _exists_:recipient.ico AND NOT recipient.ico:\"\"";
            if (recipientType.HasValue)
                query = query + " AND hints.recipientTypSubjektu:" + (int)recipientType.Value;

            if (subsidyType.HasValue)
                query = query + " AND hints.subsidyType:" + (int)subsidyType.Value;


            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }

            var res = await SubsidyRepo.Searching.SimpleSearchAsync(query, 1, 0, "666", anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(string Ico, int Count, decimal Sum)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perIco"] is BucketAggregate perIcoBa)
            {
                foreach (KeyedBucket<object> perIcoBucket in perIcoBa.Items)
                {
                    if (perIcoBucket.Key is not null && perIcoBucket.Key.ToString() is not null)
                    {
                        decimal sum = 0;
                        int count = 0;
                        if (perIcoBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                        {
                            sum = Convert.ToDecimal(sumBA.Value ?? 0);
                        }

                        if (perIcoBucket["docCount"] is ValueAggregate docCountBA)
                        {
                            count = Convert.ToInt32(docCountBA.Value ?? 0);
                        }


                        results.Add((perIcoBucket.Key.ToString(), count, sum));
                    }
                }
            }

            return results;
        }

        public static async Task<List<(string IcoPoskytovatele, decimal Sum)>> ReportPoskytovatelePoLetechAsync(
            Subsidy.Hint.Type? typDotace, int? rok)
        {
            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perProviderIco", st => st
                    .Field(f => f.SubsidyProviderIco)
                    .Size(10000)
                    .Aggregations(typeAggs => typeAggs
                        .Sum("sumAssumedAmount", sa => sa
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );

            string query = "hints.isOriginal:true AND _exists_:subsidyProviderIco AND NOT subsidyProviderIco:\"\"";
            if (typDotace.HasValue)
            {
                query += $" AND hints.subsidyType:{typDotace:D}";
            }

            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }

            var res = await SubsidyRepo.Searching.SimpleSearchAsync(
                query, 1, 0, "666",
                anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(string IcoPoskytovatele, decimal Sum)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perProviderIco"] is BucketAggregate subsidyProviderBA)
            {
                foreach (KeyedBucket<object> subsidyProviderBucket in subsidyProviderBA.Items)
                {
                    if (subsidyProviderBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                    {
                        results.Add((subsidyProviderBucket.Key.ToString(), Convert.ToDecimal(sumBA.Value ?? 0)));
                    }
                }
            }

            return results;
        }

        public static async
    Task<List<(Subsidy.Hint.CalculatedCategories Category, long Count, decimal Sum)>>
    PoKategoriichAsync(int? rok = null, int? cat = null, string ico = null)
        {
            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perCategory", st => st
                    .Field(f => f.Hints.Category1.TypeValue)
                    .Size(10000)
                        .Aggregations(typeAggs => typeAggs
                            .Sum("sumAssumedAmount", sa => sa
                                .Field(f => f.AssumedAmount)
                            )
                        )
                    );
            string query = "hints.isOriginal:true AND _exists_:recipient.ico AND NOT recipient.ico:\"\"";
            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }
            if (cat is not null && cat > 0)
            {
                query += $" AND hints.category1.typeValue:{cat}";
            }
            if (ico is not null)
            {
                query += $" AND recipient.ico:{ico}";
            }

            var res = await SubsidyRepo.Searching.SimpleSearchAsync(
                query, 1, 0, "666",
                anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(Subsidy.Hint.CalculatedCategories Category, long Count, decimal Sum)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perCategory"] is BucketAggregate subsidyCategoryBA)
            {
                foreach (KeyedBucket<object> subsidyCategoryBucket in subsidyCategoryBA.Items)
                {
                    if (int.TryParse(subsidyCategoryBucket.Key.ToString(), out int subsidyCategoryTypeVal))
                    {
                        var subsidyCategory = (Subsidy.Hint.CalculatedCategories)subsidyCategoryTypeVal;

                        if (subsidyCategoryBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                        {
                            results.Add((
                                subsidyCategory,
                                subsidyCategoryBucket.DocCount ?? 0,
                                Convert.ToDecimal(sumBA.Value ?? 0)
                                ));
                        }
                    }
                }
            }

            return results;
        }

        public static async
            Task<List<(string Ico, Subsidy.Hint.CalculatedCategories Category, long Count, decimal Sum)>>
            ReportPrijemciPoKategoriichAsync(int? rok = null, int? cat = null)
        {
            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perCategory", st => st
                    .Field(f => f.Hints.Category1.TypeValue)
                    .Size(10000)
                    .Aggregations(yearAggs => yearAggs
                        .Terms("perIco", st => st
                            .Field(f => f.Recipient.Ico)
                            .Size(100)
                            .Aggregations(typeAggs => typeAggs
                                .Sum("sumAssumedAmount", sa => sa
                                    .Field(f => f.AssumedAmount)
                                )
                            )
                        )
                    )
                );
            string query = "hints.isOriginal:true AND _exists_:recipient.ico AND NOT recipient.ico:\"\"";
            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }
            if (cat is not null && cat > 0)
            {
                query += $" AND hints.category1.typeValue:{cat}";
            }

            var res = await SubsidyRepo.Searching.SimpleSearchAsync(
                query, 1, 0, "666",
                anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(string Ico, Subsidy.Hint.CalculatedCategories Category, long Count, decimal Sum)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perCategory"] is BucketAggregate subsidyCategoryBA)
            {
                foreach (KeyedBucket<object> subsidyCategoryBucket in subsidyCategoryBA.Items)
                {
                    if (int.TryParse(subsidyCategoryBucket.Key.ToString(), out int subsidyCategoryTypeVal))
                    {
                        var subsidyCategory = (Subsidy.Hint.CalculatedCategories)subsidyCategoryTypeVal;

                        if (subsidyCategoryBucket["perIco"] is BucketAggregate subsidyIcoBA)
                        {
                            foreach (KeyedBucket<object> subsidyIcoBucket in subsidyIcoBA.Items)
                            {
                                if (subsidyIcoBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                                {
                                    results.Add((
                                        subsidyIcoBucket.Key.ToString(),
                                        subsidyCategory,
                                        subsidyIcoBucket.DocCount ?? 0,
                                        Convert.ToDecimal(sumBA.Value ?? 0)
                                        ));
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        //Příjemci dotací z více krajů
        public static async Task<List<(string Ico, int DataSourceCount, decimal SumAssumedAmmount)>>
            DotacniExperti(int? rok)
        {
            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perIco", ta => ta
                    .Field(f => f.Recipient.Ico)
                    .Size(100)
                    .Order(o => o
                        .Descending("uniqueDataSource")
                    )
                    .Aggregations(aa => aa
                        .Cardinality("uniqueDataSource", ca => ca
                            .Field(f => f.Metadata.DataSource.Suffix("keyword"))
                        )
                        .Sum("sumAssumedAmount", sa => sa
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );

            string query =
                "hints.isOriginal:true AND hints.subsidyType:2 AND _exists_:recipient.ico AND NOT recipient.ico:\"\"";

            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }

            var res = await SubsidyRepo.Searching.SimpleSearchAsync(query, 1, 0, "666", anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(string Ico, int DataSourceCount, decimal SumAssumedAmmount)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perIco"] is BucketAggregate perIcoBa)
            {
                foreach (KeyedBucket<object> perIcoBucket in perIcoBa.Items)
                {
                    if (perIcoBucket.Key is not null && perIcoBucket.Key.ToString() is not null)
                    {
                        decimal sum = 0;
                        int count = 0;
                        if (perIcoBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                        {
                            sum = Convert.ToDecimal(sumBA.Value ?? 0);
                        }

                        if (perIcoBucket["uniqueDataSource"] is ValueAggregate dataSourceCountBA)
                        {
                            count = Convert.ToInt32(dataSourceCountBA.Value ?? 0);
                        }


                        results.Add((perIcoBucket.Key.ToString(), count, sum));
                    }
                }
            }

            return results;
        }

        public static async Task<List<(string Ico, decimal SumAssumedAmmount)>>
            DotovaniSponzori(int? rok)
        {
            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perIco", t => t
                    .Field(f => f.Recipient.Ico)
                    .Size(10000)
                    .Aggregations(yearAggs => yearAggs
                        .Sum("sumAssumedAmount", sa => sa
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );

            string query =
                "hints.isOriginal:true AND hints.recipientPolitickyAngazovanySubjekt:>0 AND _exists_:recipient.ico AND NOT recipient.ico:\"\"";

            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }

            var res = await SubsidyRepo.Searching.SimpleSearchAsync(query, 1, 0, "666", anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(string Ico, decimal SumAssumedAmmount)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perIco"] is BucketAggregate perIcoBa)
            {
                foreach (KeyedBucket<object> perIcoBucket in perIcoBa.Items)
                {
                    if (perIcoBucket.Key is not null && perIcoBucket.Key.ToString() is not null)
                    {
                        decimal sum = 0;

                        if (perIcoBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                        {
                            sum = Convert.ToDecimal(sumBA.Value ?? 0);
                        }

                        results.Add((perIcoBucket.Key.ToString(), sum));
                    }
                }
            }

            return results;
        }

        public static async
            Task<List<(string ProgramName, string ProgramCode, string SubsidyProviderIco, decimal AssumedAmountSummed)>>
            TopDotacniProgramy(Subsidy.Hint.Type typDotace, int? rok)
        {
            var aggs = new AggregationContainerDescriptor<Subsidy>()
                .MultiTerms("distinct_programs", mt => mt
                    .Size(10000)
                    .Terms(t =>
                            t.Field(f => f.ProgramName.Suffix("keyword")),
                        t => t.Field(f => f.ProgramCode),
                        t => t.Field(f => f.SubsidyProviderIco)
                    )
                    .Aggregations(aa => aa
                        .Sum("sumAssumedAmount", sa => sa
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );

            string query = $"hints.isOriginal:true AND hints.subsidyType:{typDotace:D}";

            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }

            var res = await SubsidyRepo.Searching.SimpleSearchAsync(query, 1, 0, "666", anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(string ProgramName, string ProgramCode, string SubsidyProviderIco, decimal AssumedAmountSummed)>
                results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["distinct_programs"] is BucketAggregate perIcoBa)
            {
                foreach (KeyedBucket<object> perIcoBucket in perIcoBa.Items)
                {
                    if (perIcoBucket.Key is not null && perIcoBucket.Key is List<object> keyList)
                    {
                        decimal sum = 0;
                        if (perIcoBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                        {
                            sum = Convert.ToDecimal(sumBA.Value ?? 0);
                        }

                        results.Add((keyList[0].ToString(), keyList[1].ToString(), keyList[2].ToString(), sum));
                    }
                }
            }

            return results;
        }
    }
}