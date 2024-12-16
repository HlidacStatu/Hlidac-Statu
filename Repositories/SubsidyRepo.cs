using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.Searching;
using Serilog;

namespace HlidacStatu.Repositories
{
    public static partial class SubsidyRepo
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(SubsidyRepo));

        public static readonly ElasticClient SubsidyClient = Manager.GetESClient_SubsidyAsync()
            .ConfigureAwait(false).GetAwaiter().GetResult();
        

        public static async Task<Dictionary<int, Dictionary<Subsidy.Hint.Type, decimal>>> PoLetechReportAsync()
        {
            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perYear", t => t
                    .Field(f => f.Common.ApprovedYear)
                    .Size(65500)
                    .Aggregations(yearAggs => yearAggs
                        .Terms("perSubsidyType", st => st
                            .Field(f => f.Hints.SubsidyType)
                            .Size(65500)
                            .Aggregations(typeAggs => typeAggs
                                .Sum("sumAssumedAmount", sa => sa
                                    .Field(f => f.AssumedAmount)
                                )
                            )
                        )
                    )
                );

            var res = await SubsidyRepo.Searching.SimpleSearchAsync("*", 1, 0, "666", anyAggregation: aggs);
            if (res is null)
            {
                return null;
            }

            // Initialize the results dictionary
            Dictionary<int, Dictionary<Subsidy.Hint.Type, decimal>> results = new();

            // Parse the aggregation results
            if (res.ElasticResults.Aggregations["perYear"] is BucketAggregate perYearBA)
            {
                foreach (KeyedBucket<object> perYearBucket in perYearBA.Items)
                {
                    if (int.TryParse(perYearBucket.Key.ToString(), out int year))
                    {
                        results.Add(year, new Dictionary<Subsidy.Hint.Type, decimal>());
                        
                        if (perYearBucket["perSubsidyType"] is BucketAggregate subsidyTypeBA)
                        {
                            foreach (KeyedBucket<object> subsidyTypeBucket in subsidyTypeBA.Items)
                            {
                                if (Enum.TryParse<Subsidy.Hint.Type>(subsidyTypeBucket.Key.ToString(), out var subsidyType))
                                {
                                    if (subsidyTypeBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                                    {
                                        results[year].Add(subsidyType, Convert.ToDecimal(sumBA.Value ?? 0) );
                                        
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }

        public static QueryContainer AddIsNotHiddenRule(QueryContainer query)
        {
            // Create a query for `isHidden = false`
            var isHiddenQuery = new TermQuery
            {
                Field = "isHidden",
                Value = false
            };

            // Combine the original query with the `isHidden` rule
            return query == null
                ? isHiddenQuery
                : new BoolQuery
                {
                    Must = new QueryContainer[] { query, isHiddenQuery }
                };
        }
        

        //do not delete - it is used by another project
        public static async Task SaveAsync(Subsidy subsidy, bool shouldRewrite)
        {
            Logger.Debug($"Saving subsidy {subsidy.RecordNumber} from {subsidy.DataSource}/{subsidy.FileName}");
            if (subsidy is null) throw new ArgumentNullException(nameof(subsidy));

            if (!shouldRewrite)
            {
                // Check if subsidy already exists
                var existingSubsidy = await SubsidyClient.GetAsync<Subsidy>(subsidy.Id);

                //do not merge hidden one - they are to be replaced
                if (existingSubsidy.Found)
                {
                    subsidy = MergeSubsidy(existingSubsidy.Source, subsidy);
                }
            }

            subsidy.ModifiedDate = DateTime.Now;
            var res = await SubsidyClient.IndexAsync(subsidy, o => o.Id(subsidy.Id));

            if (!res.IsValid)
            {
                Logger.Error($"Failed to save subsidy for {subsidy.Id}. {res.OriginalException.Message}");
                throw new ApplicationException(res.ServerError?.ToString());
            }
            Logger.Debug($"Subsidy {subsidy.RecordNumber} from {subsidy.DataSource}/{subsidy.FileName} saved");
            
            //todo: uncomment once ready for statistic recalculation
            // if(subsidy.Common.Recipient.Ico is not null)
            //     RecalculateItemRepo.AddFirmaToProcessingQueue(subsidy.Common.Recipient.Ico, Entities.RecalculateItem.StatisticsTypeEnum.Dotace, $"VZ {subsidy.Id}");
        }

        private static Subsidy MergeSubsidy(Subsidy oldRecord, Subsidy newRecord)
        {
            Logger.Information($"Merging subsidy for {oldRecord.Id}, from {oldRecord.DataSource}/{oldRecord.FileName}, records [{newRecord.RecordNumber}] and [{oldRecord.RecordNumber}]");
            newRecord.RawData += ",\n" + oldRecord.RawData;
            newRecord.Common.SubsidyAmount += oldRecord.Common.SubsidyAmount;
            newRecord.Common.PayedAmount += oldRecord.Common.PayedAmount;
            newRecord.Common.ReturnedAmount += oldRecord.Common.ReturnedAmount;

            return newRecord;
        }

        public static async Task SetHiddenSubsidies(HashSet<string> subsidiesToHideId)
        {
            if(!subsidiesToHideId.Any())
                return;

            foreach (var subsidyToHideId in subsidiesToHideId)
            {
                var updateByQueryResponse = await SubsidyClient.UpdateByQueryAsync<Subsidy>(u => u
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                m => m.Term(t => t.Id, subsidyToHideId)
                            )
                        )
                    )
                    .Script(s => s
                        .Source("ctx._source.isHidden = true")
                    )
                );
    
                if (!updateByQueryResponse.IsValid)
                {
                    Logger.Error(updateByQueryResponse.OriginalException, $"Problem during setting hidden subsidies for {subsidiesToHideId},\n Debug info:{updateByQueryResponse.DebugInformation} ");
                }
            }
            
        }

        public static HashSet<string> GetAllIds(string datasource, string fileName)
        {
            var query = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.DataSource, datasource),
                        m => m.Term(t => t.FileName, fileName)
                    )
                );

            try
            {
                var ids = SubsidyClient.SimpleGetAllIds(5, query);
                return ids.ToHashSet();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Problem when loading ids for {datasource}/{fileName}. Aborting load", datasource, fileName);
                throw;
            }
        }
        
        public static async Task<List<Subsidy>> FindDuplicatesAsync(Subsidy subsidy)
        {
            // můžeme hledat duplicity jen u firem, lidi nedokážeme správně identifikovat
            if(string.IsNullOrWhiteSpace(subsidy.Common.Recipient.Ico))
                return null;
            
            // Build the conditional query for ProjectCode OR ProgramCode OR ProjectName
            QueryContainer projectQuery = null;

            if (!string.IsNullOrWhiteSpace(subsidy.Common.ProjectCode))
            {
                projectQuery |= new QueryContainerDescriptor<Subsidy>().Term(t => t.Common.ProjectCode.Suffix("keyword"), subsidy.Common.ProjectCode);
            }

            if (!string.IsNullOrWhiteSpace(subsidy.Common.ProgramCode))
            {
                projectQuery |= new QueryContainerDescriptor<Subsidy>().Term(t => t.Common.ProgramCode, subsidy.Common.ProgramCode);
            }

            // 255 there is because ES ignores 256+ chars when creating keywords...  
            if (!string.IsNullOrWhiteSpace(subsidy.Common.ProjectName) && subsidy.Common.ProjectName.Length <= 255 )
            {
                projectQuery |= new QueryContainerDescriptor<Subsidy>().Term(t => t.Common.ProjectName.Suffix("keyword"), subsidy.Common.ProjectName);
            }

            if (projectQuery is null)
                return null;

            // Build the main query
            var query = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Common.Recipient.Ico, subsidy.Common.Recipient.Ico),
                        m => m.Term(t => t.Common.ApprovedYear, subsidy.Common.ApprovedYear),
                        m => m.Term(t => t.AssumedAmount, subsidy.AssumedAmount),
                        m => projectQuery // Add the conditional query
                    )
                );

            try
            {
                var res = await SubsidyClient.SearchAsync<Subsidy>(s => s
                        .Size(9000)
                        .Query(q => query)
                );

                if (!res.IsValid) // try again
                {
                    await Task.Delay(500);
                    res = await SubsidyClient.SearchAsync<Subsidy>(s => s
                        .Size(9000)
                        .Query(q => query)
                    );  
                }
                
                return res.Documents.ToList();
                
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, $"Cant load duplicate subsidies for subsidyId={subsidy.Id}");
                throw;
            }
        }
        
        public static async Task<Subsidy> GetAsync(string idDotace)
        {
            if (idDotace == null) throw new ArgumentNullException(nameof(idDotace));

            var response = await SubsidyClient.GetAsync<Subsidy>(idDotace);

            return response.IsValid
                ? response.Source
                : null;
        }
        
        /// <summary>
        /// Get all subsidies. If query is null, then it matches all except hidden ones
        /// </summary>
        /// <param name="query"></param>
        /// <param name="scrollTimeout"></param>
        /// <param name="scrollSize"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async IAsyncEnumerable<Subsidy> GetAllAsync(QueryContainer query,
            bool excludeHidden = true,
            string scrollTimeout = "5m",
            int scrollSize = 300)
        {
            if (excludeHidden)
            {
                query = AddIsNotHiddenRule(query);
            }

            ISearchResponse<Subsidy> initialResponse = null;
            if (query is null && excludeHidden)
            {
                initialResponse = await SubsidyClient.SearchAsync<Subsidy>(scr => scr
                    .From(0)
                    .Take(scrollSize)
                    .Query(q => AddIsNotHiddenRule(null))
                    .Scroll(scrollTimeout));
            }
            else if (query is null && !excludeHidden)
            {
                initialResponse = await SubsidyClient.SearchAsync<Subsidy>(scr => scr
                    .From(0)
                    .Take(scrollSize)
                    .MatchAll()
                    .Scroll(scrollTimeout));
            }
            else
            {
                initialResponse = await SubsidyClient.SearchAsync<Subsidy>(scr => scr
                    .From(0)
                    .Take(scrollSize)
                    .Query(q => query)
                    .Scroll(scrollTimeout));
            }

            if (!initialResponse.IsValid || string.IsNullOrEmpty(initialResponse.ScrollId))
                throw new Exception(initialResponse.ServerError.Error.Reason);

            if (initialResponse.Documents.Any())
                foreach (var dotace in initialResponse.Documents)
                {
                    yield return dotace;
                }

            string scrollid = initialResponse.ScrollId;
            bool isScrollSetHasData = true;
            while (isScrollSetHasData)
            {
                ISearchResponse<Subsidy> loopingResponse = await SubsidyClient.ScrollAsync<Subsidy>(scrollTimeout, scrollid);
                if (loopingResponse.IsValid)
                {
                    foreach (var dotace in loopingResponse.Documents)
                    {
                        yield return dotace;
                    }
                    scrollid = loopingResponse.ScrollId;
                }
                isScrollSetHasData = loopingResponse.Documents.Any();
            }

            _ = await SubsidyClient.ClearScrollAsync(new ClearScrollRequest(scrollid));
        }
        
        public static IAsyncEnumerable<Subsidy> GetDotaceForIcoAsync(string ico)
        {
            QueryContainer qc = new QueryContainerDescriptor<Subsidy>()
                .Term(f => f.Common.Recipient.Ico, ico);

            return GetAllAsync(qc);
        }

    }
}