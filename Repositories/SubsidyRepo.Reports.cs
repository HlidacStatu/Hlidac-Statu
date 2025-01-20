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
        public class ProgramStatistics
        {
            public int Year { get; set; }
            public string ProviderIco { get; set; }
            public Subsidy.Hint.Type SubsidyType { get; set; }
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
            
            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
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

            string query = "hints.isOriginal:true";

            if (!string.IsNullOrWhiteSpace(programName) || !string.IsNullOrWhiteSpace(programCode))
            {
                query += " AND (";
                if (!string.IsNullOrWhiteSpace(programName))
                    query += $"programName.keyword:\"{programName}\"";

                if (!string.IsNullOrWhiteSpace(programName) && !string.IsNullOrWhiteSpace(programCode))
                    query += " OR ";

                if (!string.IsNullOrWhiteSpace(programCode))
                    query += $"programCode:\"{programCode}\"";

                query += ")";
            }
            
            var res = await SubsidyRepo.Searching.SimpleSearchAsync(query, 1, 0,
                Repositories.Searching.SubsidySearchResult.SubsidyOrderResult.FastestForScroll,
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
                                        
                                        if (Enum.TryParse<Subsidy.Hint.Type>(subsidyTypeBucket.Key.ToString(),
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
            string query = "hints.isOriginal:true AND _exists_:recipient.ico AND NOT recipient.ico:\"\"";
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
        public static async Task<List<(string Ico, int Count, decimal Sum)>> ReportTopPrijemciAsync(string query, int numOfItems = 100)
        {

            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perIco", t => t
                    .Field(f => f.Recipient.Ico)
                    .Size(numOfItems)
                    .Aggregations(typeAggs => typeAggs
                        .Sum("sumAssumedAmount", sa => sa
                            .Field(f => f.AssumedAmount)
                        )
                        .ValueCount("docCount", vc => vc
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );



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