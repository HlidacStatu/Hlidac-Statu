using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;

namespace HlidacStatu.Repositories
{
    public static partial class DotaceRepo
    {
        public class ProgramStatistics
        {
            public int Year { get; set; }
            public string ProviderIco { get; set; }
            public Dotace.Hint.Type SubsidyType { get; set; }
            public int RecipientIcoCount { get; set; }
            public decimal SumAssumedAmount { get; set; }
            public decimal MinAssumedAmount { get; set; }
            public decimal MaxAssumedAmount { get; set; }
            public decimal AvgAssumedAmount { get; set; }
            public int CountGrantedCompaniesFirstYear { get; set; }
            public int CountPolitickyAngazovanySubjekt { get; set; }
            
        }
        
        public static readonly int[] DefaultLimitedYears = Enumerable.Range(2010, DateTime.Now.Year - 2010).ToArray();
        public static readonly int[] DefaultKrajskeLimitedYears = Enumerable.Range(2021, DateTime.Now.Year - 2010).ToArray();

        public static async Task<List<ProgramStatistics>> ProgramStatisticAsync(string programName, string programCode)
        {
            
            AggregationContainerDescriptor<Dotace> aggs = new AggregationContainerDescriptor<Dotace>()
                .Terms("perYear", t => t
                    .Field(f => f.ApprovedYear)
                    .Size(10000)
                    .Aggregations(yearAggs => yearAggs
                        .Terms("perSubsidyType", st => st
                            .Field(f => f.Hints.SubsidyType)
                            .Size(10000)
                            .Aggregations(stypeAggs => stypeAggs
                                .Terms("perProviderIco", sty => sty
                                    .Field(f => f.SubsidyProviderIco)
                                    .Size(10000)
                                    .Aggregations(providerAggs => providerAggs
                                        .Sum("sumAssumedAmount", sa => sa
                                            .Field(f => f.AssumedAmount)
                                        )
                                        .Min("minAssumedAmount", sa => sa
                                            .Field(f => f.AssumedAmount)
                                        )
                                        .Max("maxAssumedAmount", sa => sa
                                            .Field(f => f.AssumedAmount)
                                        )
                                        .Average("avgAssumedAmount", sa => sa
                                            .Field(f => f.AssumedAmount)
                                        )
                                        .ValueCount("recipientCount", vc => vc
                                            .Field(f => f.Recipient.Ico)
                                        )
                                        .Filter("recipientPocetLetOdZalozeniOne", filter => filter
                                            .Filter(flt => flt
                                                .Term(r => r
                                                    .Field(f => f.Hints.RecipientPocetLetOdZalozeni)
                                                    .Value(1)
                                                )
                                            )
                                        )
                                        .Filter("recipientPolitickyAngazovanySubjektPositive", filter => filter
                                            .Filter(flt => flt
                                                .Range(r => r
                                                    .Field(f => f.Hints.RecipientPolitickyAngazovanySubjekt)
                                                    .GreaterThan(0)
                                                )
                                            )
                                        )
                                    )
                                )
                                
                            )
                        )
                    )
                );

            string query = "";

            if (!string.IsNullOrWhiteSpace(programName) || !string.IsNullOrWhiteSpace(programCode))
            {
                if (!string.IsNullOrWhiteSpace(programName))
                    query += $"programName.keyword:\"{programName}\"";

                if (!string.IsNullOrWhiteSpace(programName) && !string.IsNullOrWhiteSpace(programCode))
                    query += " AND ";

                if (!string.IsNullOrWhiteSpace(programCode))
                    query += $"programCode:\"{programCode}\"";
            }
            
            var res = await DotaceRepo.Searching.SimpleSearchAsync(query, 1, 0,
                Repositories.Searching.DotaceSearchResult.DotaceOrderResult.FastestForScroll,
                anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<ProgramStatistics> results = new();

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
                                if (subsidyTypeBucket["perProviderIco"] is BucketAggregate providerIcoBA)
                                {
                                    foreach (KeyedBucket<object> providerIcoBucket in providerIcoBA.Items)
                                    {
                                        var programStat = new ProgramStatistics()
                                        {
                                            Year = year,
                                            ProviderIco = providerIcoBucket.Key.ToString(),
                                        };
                                        results.Add(programStat);
                                        
                                        if (Enum.TryParse<Dotace.Hint.Type>(subsidyTypeBucket.Key.ToString(),
                                                out var subsidyType))
                                        {
                                            programStat.SubsidyType = subsidyType;
                                        }
                                        
                                        if (providerIcoBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                                        {
                                            programStat.SumAssumedAmount = Convert.ToDecimal(sumBA.Value ?? 0);
                                        }
                                        
                                        if (providerIcoBucket["minAssumedAmount"] is ValueAggregate minBA)
                                        {
                                            programStat.MinAssumedAmount = Convert.ToDecimal(minBA.Value ?? 0);
                                        }
                                        
                                        if (providerIcoBucket["maxAssumedAmount"] is ValueAggregate maxBA)
                                        {
                                            programStat.MaxAssumedAmount = Convert.ToDecimal(maxBA.Value ?? 0);
                                        }
                                        
                                        if (providerIcoBucket["avgAssumedAmount"] is ValueAggregate avgBA)
                                        {
                                            programStat.AvgAssumedAmount = Convert.ToDecimal(avgBA.Value ?? 0);
                                        }
                                        
                                        if (providerIcoBucket["recipientCount"] is ValueAggregate recCount)
                                        {
                                            programStat.RecipientIcoCount = Convert.ToInt32(recCount.Value ?? 0);
                                        }
                                        
                                        if (providerIcoBucket["recipientPocetLetOdZalozeniOne"] is SingleBucketAggregate recFirstYear)
                                        {
                                            programStat.CountGrantedCompaniesFirstYear = Convert.ToInt32(recFirstYear.DocCount);
                                        }
                                        
                                        if (providerIcoBucket["recipientPolitickyAngazovanySubjektPositive"] is SingleBucketAggregate recPolitical)
                                        {
                                            programStat.CountPolitickyAngazovanySubjekt = Convert.ToInt32(recPolitical.DocCount);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

                
            return results;
        }

        public static async Task<List<(int Year, Dotace.Hint.Type SubsidyType, decimal Sum)>> ReportPoLetechAsync(
            bool limitYears = true, int[] limitedYears = null)
        {
            limitedYears = limitedYears ?? Enumerable.Range(2010, DateTime.Now.Year - 2010).ToArray();

            AggregationContainerDescriptor<Dotace> aggs = new AggregationContainerDescriptor<Dotace>()
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

            var res = await DotaceRepo.Searching.SimpleSearchAsync("*", 1, 0, "666",
                anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(int Year, Dotace.Hint.Type SubsidyType, decimal Sum)> results = new();

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
                                if (Enum.TryParse<Dotace.Hint.Type>(subsidyTypeBucket.Key.ToString(),
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

        public static async Task<List<(string Ico, long Count, decimal Sum)>> ReportTopPrijemciAsync(int? rok,
            Firma.TypSubjektuEnum? recipientType = null,
            Dotace.Hint.Type? subsidyType = null)
        {
            string query = "_exists_:recipient.ico AND NOT recipient.ico:\"\"";
            if (recipientType.HasValue)
                query = query + " AND hints.recipientTypSubjektu:" + (int)recipientType.Value;

            if (subsidyType.HasValue)
                query = query + " AND hints.subsidyType:" + (int)subsidyType.Value;


            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }
            return await ReportTopPrijemciAsync(query);
        }
        public static async Task<List<(string Ico, long Count, decimal Sum)>> ReportTopPrijemciAsync(string query, int numOfItems = 100)
        {

            AggregationContainerDescriptor<Dotace> aggs = new AggregationContainerDescriptor<Dotace>()
                .Terms("perIco", t => t
                    .Field(f => f.Recipient.Ico)
                    .Size(numOfItems)
                    .Aggregations(typeAggs => typeAggs
                        .Sum("sumAssumedAmount", sa => sa
                            .Field(f => f.AssumedAmount)
                        )
                    )
                    .Order(o=>o.Descending("sumAssumedAmount"))
                );



            var res = await DotaceRepo.Searching.SimpleSearchAsync(query, 1, 0, "666", anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(string Ico, long Count, decimal Sum)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perIco"] is BucketAggregate perIcoBa)
            {
                foreach (KeyedBucket<object> perIcoBucket in perIcoBa.Items)
                {
                    if (perIcoBucket.Key is not null && perIcoBucket.Key.ToString() is not null)
                    {
                        decimal sum = 0;
                        long count = 0;
                        if (perIcoBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                        {
                            sum = Convert.ToDecimal(sumBA.Value ?? 0);
                        }
                        
                        count = perIcoBucket.DocCount ?? 0;

                        results.Add((perIcoBucket.Key.ToString(), count, sum));
                    }
                }
            }

            return results;
        }

        public static async Task<List<(string IcoPoskytovatele, long Count, decimal Sum)>> ReportPoskytovatelePoLetechAsync(
            Dotace.Hint.Type? typDotace, int? rok)
        {
            AggregationContainerDescriptor<Dotace> aggs = new AggregationContainerDescriptor<Dotace>()
                .Terms("perProviderIco", st => st
                    .Field(f => f.SubsidyProviderIco)
                    .Size(10000)
                    .Aggregations(typeAggs => typeAggs
                        .Sum("sumAssumedAmount", sa => sa
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );

            string query = "_exists_:subsidyProviderIco AND NOT subsidyProviderIco:\"\"";
            if (typDotace.HasValue)
            {
                query += $" AND hints.subsidyType:{typDotace:D}";
            }

            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }

            var res = await DotaceRepo.Searching.SimpleSearchAsync(
                query, 1, 0, "666",
                anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(string IcoPoskytovatele, long Count, decimal Sum)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perProviderIco"] is BucketAggregate subsidyProviderBA)
            {
                foreach (KeyedBucket<object> subsidyProviderBucket in subsidyProviderBA.Items)
                {
                    if (subsidyProviderBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                    {
                        results.Add((subsidyProviderBucket.Key.ToString(), subsidyProviderBucket.DocCount ?? 0, Convert.ToDecimal(sumBA.Value ?? 0)));
                    }
                }
            }

            return results;
        }
        
        public static async Task<List<(int Year, long Count, decimal Sum)>> SumyPoskytnutychDotaciPoLetechAsync(string providerIco)
        {
            AggregationContainerDescriptor<Dotace> aggs = new AggregationContainerDescriptor<Dotace>()
                .Terms("perYear", st => st
                    .Field(f => f.ApprovedYear)
                    .Size(10000)
                    .Aggregations(typeAggs => typeAggs
                        .Sum("sumAssumedAmount", sa => sa
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );

            string query = $"subsidyProviderIco:{providerIco}";

            var res = await DotaceRepo.Searching.SimpleSearchAsync(
                query, 1, 0, "666",
                anyAggregation: aggs);
            if (res is null)
            {
                return [];
            }

            // Initialize the results dictionary
            List<(int Year, long Count, decimal Sum)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perYear"] is BucketAggregate subsidyYearBA)
            {
                foreach (KeyedBucket<object> subsidyYearBucket in subsidyYearBA.Items)
                {
                    if (subsidyYearBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                    {
                        if (int.TryParse(subsidyYearBucket.Key.ToString(), out int year))
                        {
                            results.Add((year, subsidyYearBucket.DocCount ?? 0, Convert.ToDecimal(sumBA.Value ?? 0)));    
                        }
                        
                    }
                }
            }

            return results;
        }

        public static async
    Task<List<(Dotace.Hint.CalculatedCategories Category, long Count, decimal Sum)>>
    PoKategoriichAsync(int? rok = null, int? cat = null, string ico = null)
        {
            AggregationContainerDescriptor<Dotace> aggs = new AggregationContainerDescriptor<Dotace>()
                .Terms("perCategory", st => st
                    .Field(f => f.Hints.Category1.TypeValue)
                    .Size(10000)
                        .Aggregations(typeAggs => typeAggs
                            .Sum("sumAssumedAmount", sa => sa
                                .Field(f => f.AssumedAmount)
                            )
                        )
                    );
            string query = "_exists_:recipient.ico AND NOT recipient.ico:\"\"";
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

            var res = await DotaceRepo.Searching.SimpleSearchAsync(
                query, 1, 0, "666",
                anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(Dotace.Hint.CalculatedCategories Category, long Count, decimal Sum)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perCategory"] is BucketAggregate subsidyCategoryBA)
            {
                foreach (KeyedBucket<object> subsidyCategoryBucket in subsidyCategoryBA.Items)
                {
                    if (int.TryParse(subsidyCategoryBucket.Key.ToString(), out int subsidyCategoryTypeVal))
                    {
                        var subsidyCategory = (Dotace.Hint.CalculatedCategories)subsidyCategoryTypeVal;

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
            Task<List<(string Ico, Dotace.Hint.CalculatedCategories Category, long Count, decimal Sum)>>
            ReportPrijemciPoKategoriichAsync(int? rok = null, int? cat = null)
        {
            AggregationContainerDescriptor<Dotace> aggs = new AggregationContainerDescriptor<Dotace>()
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
            string query = "_exists_:recipient.ico AND NOT recipient.ico:\"\"";
            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }
            if (cat is not null && cat > 0)
            {
                query += $" AND hints.category1.typeValue:{cat}";
            }

            var res = await DotaceRepo.Searching.SimpleSearchAsync(
                query, 1, 0, "666",
                anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            List<(string Ico, Dotace.Hint.CalculatedCategories Category, long Count, decimal Sum)> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perCategory"] is BucketAggregate subsidyCategoryBA)
            {
                foreach (KeyedBucket<object> subsidyCategoryBucket in subsidyCategoryBA.Items)
                {
                    if (int.TryParse(subsidyCategoryBucket.Key.ToString(), out int subsidyCategoryTypeVal))
                    {
                        var subsidyCategory = (Dotace.Hint.CalculatedCategories)subsidyCategoryTypeVal;

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
            AggregationContainerDescriptor<Dotace> aggs = new AggregationContainerDescriptor<Dotace>()
                .Terms("perIco", ta => ta
                    .Field(f => f.Recipient.Ico)
                    .Size(100)
                    .Order(o => o
                        .Descending("uniqueDataSource")
                    )
                    .Aggregations(aa => aa
                        .Cardinality("uniqueDataSource", ca => ca
                            .Field(f => f.PrimaryDataSource.Suffix("keyword"))
                        )
                        .Sum("sumAssumedAmount", sa => sa
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );

            string query = "hints.subsidyType:2 AND _exists_:recipient.ico AND NOT recipient.ico:\"\"";

            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }

            var res = await DotaceRepo.Searching.SimpleSearchAsync(query, 1, 0, "666", anyAggregation: aggs);
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
            AggregationContainerDescriptor<Dotace> aggs = new AggregationContainerDescriptor<Dotace>()
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
                "hints.recipientPolitickyAngazovanySubjekt:>0 AND _exists_:recipient.ico AND NOT recipient.ico:\"\"";

            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }

            var res = await DotaceRepo.Searching.SimpleSearchAsync(query, 1, 0, "666", anyAggregation: aggs);
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
            TopDotacniProgramy(Dotace.Hint.Type typDotace, int? rok)
        {
            var aggs = new AggregationContainerDescriptor<Dotace>()
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

            string query = $"hints.subsidyType:{typDotace:D}";

            if (rok is not null && rok > 0)
            {
                query += $" AND approvedYear:{rok}";
            }

            var res = await DotaceRepo.Searching.SimpleSearchAsync(query, 1, 0, "666", anyAggregation: aggs);
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