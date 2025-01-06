using Nest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Repositories.Searching;
using Serilog;

// using Newtonsoft.Json;

namespace HlidacStatu.Repositories
{
    public static partial class SubsidyRepo
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(SubsidyRepo));

        public static readonly ElasticClient SubsidyClient = Manager.GetESClient_SubsidyAsync()
            .ConfigureAwait(false).GetAwaiter().GetResult();

        public static readonly string[] SubsidyDuplicityOrdering =
            ["IsRed", "Cedr", "Eufondy", "Státní zemědělský intervenční fond"];

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _subsidyLocks = new();


        public static async Task<Dictionary<int, Dictionary<Subsidy.Hint.Type, decimal>>> PoLetechReportAsync()
        {
            AggregationContainerDescriptor<Subsidy> aggs = new AggregationContainerDescriptor<Subsidy>()
                .Terms("perYear", t => t
                    .Field(f => f.ApprovedYear)
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
                                if (Enum.TryParse<Subsidy.Hint.Type>(subsidyTypeBucket.Key.ToString(),
                                        out var subsidyType))
                                {
                                    if (subsidyTypeBucket["sumAssumedAmount"] is ValueAggregate sumBA)
                                    {
                                        results[year].Add(subsidyType, Convert.ToDecimal(sumBA.Value ?? 0));
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
                Field = "metadata.isHidden",
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


        public static async Task SaveAsync(Subsidy subsidy, bool shouldRewrite)
        {
            // because of setting duplicates, this method can run only for one set of subsidies at the time
            // otherwise, duplicates could rewrite/saved subsidies (RACE CONDITION)
            // since duplicates are loaded/saved mainly for batches with the same "DuplaHash", 
            // then we can afford to lock on that level and widen the bottleneck

            var semaphore = _subsidyLocks.GetOrAdd(subsidy.DuplaHash, _ => new SemaphoreSlim(1, 1));

            Logger.Debug($"Waiting to acquire semaphore for subsidy {subsidy.Id}");
            await semaphore.WaitAsync();
            try
            {
                Logger.Debug($"Semaphore acquired for subsidy {subsidy.Id}");
                await SaveThreadUnsafeAsync(subsidy, shouldRewrite);
                await FindAndSetDuplicatesThreadUnsafeAsync(subsidy);
            }
            finally
            {
                Logger.Debug($"Releasing semaphore for subsidy {subsidy.Id}");
                semaphore.Release();
            }
        }

        private static async Task SaveThreadUnsafeAsync(Subsidy subsidy, bool shouldRewrite)
        {
            Logger.Debug(
                $"Saving subsidy {subsidy.Metadata.RecordNumber} from {subsidy.Metadata.DataSource}/{subsidy.Metadata.FileName}");
            if (subsidy is null) throw new ArgumentNullException(nameof(subsidy));

            if (!shouldRewrite)
            {
                // Check if subsidy already exists
                var existingSubsidy = await SubsidyClient.GetAsync<Subsidy>(subsidy.Id);

                //do not merge hidden one - they are to be replaced
                // - update hidden subsidies are controlled by shouldRewrite
                if (existingSubsidy.Found)
                {
                    subsidy = MergeSubsidy(existingSubsidy.Source, subsidy);
                }
            }

            await SaveSubsidyDirectlyToEs(subsidy);

            Logger.Debug(
                $"Subsidy {subsidy.Metadata.RecordNumber} from {subsidy.Metadata.DataSource}/{subsidy.Metadata.FileName} saved");

            //todo: uncomment once ready for statistic recalculation
            // if(subsidy.Recipient.Ico is not null)
            //     RecalculateItemRepo.AddFirmaToProcessingQueue(subsidy.Recipient.Ico, Entities.RecalculateItem.StatisticsTypeEnum.Dotace, $"VZ {subsidy.Id}");
        }

        private static Subsidy MergeSubsidy(Subsidy oldRecord, Subsidy newRecord)
        {
            Logger.Information(
                $"Merging subsidy for {oldRecord.Id}, from {oldRecord.Metadata.DataSource}/{oldRecord.Metadata.FileName}, records [{newRecord.Metadata.RecordNumber}] and [{oldRecord.Metadata.RecordNumber}]");
            newRecord.SubsidyAmount += oldRecord.SubsidyAmount;
            newRecord.PayedAmount += oldRecord.PayedAmount;
            newRecord.ReturnedAmount += oldRecord.ReturnedAmount;
            newRecord.Rozhodnuti.AddRange(oldRecord.Rozhodnuti);
            newRecord.Cerpani.AddRange(oldRecord.Cerpani);
            newRecord.RawData.AddRange(oldRecord.RawData);

            return newRecord;
        }

        public static HashSet<string> GetAllIds(string datasource, string fileName)
        {
            var query = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Metadata.DataSource.Suffix("keyword"), datasource),
                        m => m.Term(t => t.Metadata.FileName, fileName)
                    )
                );

            try
            {
                var ids = SubsidyClient.SimpleGetAllIds(5, query);
                return ids.ToHashSet();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex, "Problem when loading ids for {datasource}/{fileName}. Aborting load", datasource,
                    fileName);
                throw;
            }
        }

        static Subsidy.SubsidyComparer subsidyComparer = new();
        public static async Task FindAndSetDuplicatesThreadUnsafeAsync(Subsidy baseSubsidy)
        {
            // můžeme hledat duplicity jen u firem, lidi nedokážeme správně identifikovat
            if (string.IsNullOrWhiteSpace(baseSubsidy.Recipient.Ico))
                return;

            var allSubsidies = await FindDuplicatesAsync(baseSubsidy); // err: proč zase ids
            //no duplicates exist 
            if (allSubsidies == null || !allSubsidies.Any())
            {
                Logger.Error($"For subsidy [{baseSubsidy.Id}] was not found anything.");
                return;
            }
            //if baseIs Missing in allSubsidies
            if (allSubsidies.Any(m => m.Id == baseSubsidy.Id) == false)
                allSubsidies.Add(baseSubsidy);

            allSubsidies = allSubsidies.Distinct(subsidyComparer).ToList();

            //consolidate base on ids
            var subsidyIds = allSubsidies.SelectMany(m => m.Hints.Duplicates)
                .Concat(allSubsidies.SelectMany(m => m.Hints.HiddenDuplicates))
                .Distinct()
                .ToList();

            foreach (var id in subsidyIds)
            {
                //add missing
                if (!allSubsidies.Any(m => m.Id == id))
                {
                    var s = await GetAsync(id);
                    if (s != null)
                    {
                        allSubsidies.Add(s);
                    }
                }
            }
            await ConsolidatedAndSetDuplicatesThreadUnsafeAsync(allSubsidies);
        }
        public static async Task ConsolidatedAndSetDuplicatesThreadUnsafeAsync(params Subsidy[] subsidies)
        {
            await ConsolidatedAndSetDuplicatesThreadUnsafeAsync(new List<Subsidy>(subsidies));
        }
        public static async Task ConsolidatedAndSetDuplicatesThreadUnsafeAsync(List<Subsidy> subsidies)
        {
        //load all initial subsidies

        reload:
            var subsidyIds = subsidies.Where(m => m.Hints?.HasDuplicates == true).SelectMany(m => m.Hints.Duplicates)
                .Concat(subsidies.Where(m => m.Hints?.HasHiddenDuplicates == true).SelectMany(m => m.Hints.HiddenDuplicates))
                .Distinct()
                .ToList();

            bool added = false;
            foreach (var id in subsidyIds)
            {
                //add missing
                if (subsidies.Any(m => m.Id == id) == false)
                {
                    var s = await GetAsync(id);
                    if (s != null)
                    {
                        subsidies.Add(s);
                        added = true;
                    }
                }
            }

            if (added)
                goto reload;

            await SetDuplicatesThreadUnsafeAsync(subsidies);
        }


        public static async Task SetDuplicatesThreadUnsafeAsync(List<Subsidy> allSubsidies)
        {

            var visibleDuplicates = allSubsidies //subsidies ordered by sourceOrdering
                .Where(s => s.Metadata.IsHidden == false)
                .OrderBy(s => Array.IndexOf(SubsidyDuplicityOrdering, s.Metadata.DataSource) >= 0
                    ? Array.IndexOf(SubsidyDuplicityOrdering, s.Metadata.DataSource)
                    : 9999)
                .ToList();

            var hiddenDuplicates = allSubsidies //hidden subsidies
                .Where(s => s.Metadata.IsHidden == true)
                .ToList();

            var visibleDuplicateIds = visibleDuplicates.Select(s => s.Id).ToList();
            var hiddenDuplicateIds = hiddenDuplicates.Select(hs => hs.Id).ToList();

            Subsidy original = null;
            foreach (var visibleSubsidy in visibleDuplicates)
            {
                // set duplicates
                if (original == null)
                {
                    original = visibleSubsidy;
                }

                visibleSubsidy.Hints.SetDuplicate(visibleSubsidy, visibleDuplicateIds, hiddenDuplicateIds, original.Id);
            }

            foreach (var hiddenSubsidy in hiddenDuplicates)
            {
                // set duplicates - if there is no visible original, then we need to set hidden original
                if (original == null)
                {
                    original = hiddenSubsidy;
                }

                hiddenSubsidy.Hints.SetDuplicate(hiddenSubsidy, visibleDuplicateIds, hiddenDuplicateIds, original.Id);
            }

            await BulkSaveSubsidiesToEs(allSubsidies);
        }

        public static async Task UpdateAndSaveSubsidyDuplicates(string originalSubsidyId, IEnumerable<string> visibleDuplicates, IEnumerable<string> hiddenDuplicates)
        {
        }
        public static async Task SaveSubsidyDirectlyToEs(Subsidy subsidy)
        {
            subsidy.Metadata.ModifiedDate = DateTime.Now;

            var res = await SubsidyClient.IndexAsync(subsidy, o => o.Id(subsidy.Id));

            if (!res.IsValid)
            {
                Logger.Error($"Failed to save subsidy for {subsidy.Id}. {res.OriginalException.Message}");
                throw new ApplicationException(res.ServerError?.ToString());
            }
        }

        private static async Task BulkSaveSubsidiesToEs(List<Subsidy> subsidies)
        {
            var bulkDescriptor = new BulkDescriptor();

            foreach (var subsidy in subsidies)
            {
                subsidy.Metadata.ModifiedDate = DateTime.Now;

                bulkDescriptor.Index<Subsidy>(op => op
                    .Id(subsidy.Id)
                    .Document(subsidy));
            }

            var res = await SubsidyClient.BulkAsync(bulkDescriptor);

            if (!res.IsValid || res.Errors)
            {
                foreach (var item in res.ItemsWithErrors)
                {
                    Logger.Error($"Failed to save subsidy for {item.Id}. Error: {item.Error.Reason}");
                }

                throw new ApplicationException("Bulk save operation encountered errors.");
            }
        }

        public static async Task<List<Subsidy>> FindDuplicatesAsync(Subsidy subsidy)
        {
            // Build the conditional query for ProjectCode OR ProgramCode OR ProjectName
            QueryContainer projectQuery = null;
            if (!string.IsNullOrWhiteSpace(subsidy.ProjectCodeHash))
            {
                projectQuery |=
                    new QueryContainerDescriptor<Subsidy>().Term(t => t.ProjectCodeHash, subsidy.ProjectCodeHash);
            }

            if (!string.IsNullOrWhiteSpace(subsidy.ProjectNameHash))
            {
                projectQuery |=
                    new QueryContainerDescriptor<Subsidy>().Term(t => t.ProjectNameHash, subsidy.ProjectNameHash);
            }

            if (projectQuery is null)
                return null;

            // Build the main query
            var query = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.DuplaHash, subsidy.DuplaHash),
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
                Logger.Error(ex, $"Cant load duplicate subsidies for subsidyId={subsidy.Id}");
                throw;
            }
        }
        public static async Task<bool> ExistsAsync(string idDotace)
        {
            if (idDotace == null) throw new ArgumentNullException(nameof(idDotace));

            var response = await SubsidyClient.DocumentExistsAsync<Subsidy>(idDotace);

            return response.Exists;
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
                ISearchResponse<Subsidy> loopingResponse =
                    await SubsidyClient.ScrollAsync<Subsidy>(scrollTimeout, scrollid);
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
                .Term(f => f.Recipient.Ico, ico);

            return GetAllAsync(qc);
        }
    }
}