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
using HlidacStatu.Extensions;

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
        private static readonly SemaphoreSlim _deleteLock = new SemaphoreSlim(1, 1);
        

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

        public static async Task<List<Subsidy>> FindAllDuplicateReferers(string subsidyId)
        {
            var query = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                        .Should(
                            s => s.Term(t => t
                                .Field(f => f.Hints.Duplicates.Suffix("keyword"))
                                .Value(subsidyId)
                            ),
                            s => s.Term(t => t
                                .Field(f => f.Hints.HiddenDuplicates.Suffix("keyword"))
                                .Value(subsidyId)
                            )
                        )
                        .MinimumShouldMatch(1) // At least one of the conditions must be true
                );

            try
            {
                var res = await SubsidyClient.SearchAsync<Subsidy>(s => s
                    .Size(10000)
                    .Query(q => query)
                );

                if (!res.IsValid) // try again
                {
                    await Task.Delay(500);
                    res = await SubsidyClient.SearchAsync<Subsidy>(s => s
                        .Size(10000)
                        .Query(q => query)
                    );
                }

                return res.Documents.ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Cant load duplicate referers for subsidyId={subsidyId}");
                throw;
            }
        }

        public static async Task DeleteAsync(string subsidyId)
        {
            Logger.Debug($"Removing subsidy {subsidyId} and all references to it.");
            await _deleteLock.WaitAsync();
            try
            {
                var subsidy = await GetAsync(subsidyId);
                var referers = await FindAllDuplicateReferers(subsidyId);

                string newOriginalId = null;
                if (subsidy.Hints.IsOriginal)
                {
                    newOriginalId = subsidy.Hints.Duplicates.FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(newOriginalId) && referers.Any())
                    {
                        newOriginalId = referers.Where(r => r.Metadata.IsHidden == false)
                            .OrderBy(s => Array.IndexOf(SubsidyDuplicityOrdering, s.Metadata.DataSource) >= 0
                                ? Array.IndexOf(SubsidyDuplicityOrdering, s.Metadata.DataSource)
                                : 9999)
                            .FirstOrDefault()?.Id;
                    }
                }

                if (referers.Any())
                {
                    foreach (var referer in referers)
                    {
                        referer.Hints.Duplicates.Remove(subsidyId);
                        referer.Hints.HiddenDuplicates.Remove(subsidyId);
                        if (!string.IsNullOrWhiteSpace(newOriginalId))
                        {
                            if (referer.Id == newOriginalId)
                            {
                                referer.Hints.IsOriginal = true;
                            }
                            else
                            {
                                referer.Hints.OriginalSubsidyId = newOriginalId;
                            }
                        }

                        // Console.WriteLine("saving referer");
                        await SaveSubsidyDirectlyToEs(referer);
                    }
                }

                // Console.WriteLine("deleting subsidy");
                await SubsidyClient.DeleteAsync<Subsidy>(subsidyId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Cant delete subsidy {subsidyId}");
                throw;
            }
            finally
            {
                _deleteLock.Release();
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
            var allSubsidies = await FindDuplicatesAsync(baseSubsidy);
            //no duplicates exist 
            if (allSubsidies == null || !allSubsidies.Any())
            {
                return;
            }

            //if baseIs Missing in allSubsidies
            if (allSubsidies.Any(m => m.Id == baseSubsidy.Id) == false)
                allSubsidies.Add(baseSubsidy);

            //consolidate base on ids (find duplicates of duplicates)
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
                .Concat(subsidies.Where(m => m.Hints?.HasHiddenDuplicates == true)
                    .SelectMany(m => m.Hints.HiddenDuplicates))
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

                SetDuplicate(visibleSubsidy, visibleDuplicateIds, hiddenDuplicateIds, original.Id);
            }

            foreach (var hiddenSubsidy in hiddenDuplicates)
            {
                // set duplicates - if there is no visible original, then we need to set hidden original
                if (original == null)
                {
                    original = hiddenSubsidy;
                }

                SetDuplicate(hiddenSubsidy, visibleDuplicateIds, hiddenDuplicateIds, original.Id);
            }

            await BulkSaveSubsidiesToEs(allSubsidies);
        }
        
        private static void SetDuplicate(Subsidy subsidy, List<string> duplicates, List<string> hiddenDuplicates, string originalSubsidyId)
        {
            if(string.IsNullOrEmpty(originalSubsidyId))
                return;
                
            if (originalSubsidyId == subsidy.Id)
            {
                //set as original
                subsidy.Hints.IsOriginal = true;
                subsidy.Hints.OriginalSubsidyId = null;
                subsidy.Hints.DuplicateCalculated = DateTime.Now;
                subsidy.Hints.Duplicates = duplicates.Where(d => d != subsidy.Id).ToList();
                subsidy.Hints.HiddenDuplicates = hiddenDuplicates.Where(d => d != subsidy.Id).ToList();;

            }
            else
            {
                //set as duplicate
                subsidy.Hints.IsOriginal = false;
                subsidy.Hints.OriginalSubsidyId = originalSubsidyId; 
                subsidy.Hints.DuplicateCalculated = DateTime.Now;
                subsidy.Hints.Duplicates = duplicates.Where(d => d != subsidy.Id).ToList();
                subsidy.Hints.HiddenDuplicates = hiddenDuplicates.Where(d => d != subsidy.Id).ToList();
            }
        }

        private static async Task SaveSubsidyDirectlyToEs(Subsidy subsidy)
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
            var hashDuplicateTask = FindDuplicatesByHashAsync(subsidy);
            var originalIdDuplicateTask = FindDuplicatesByOriginalIdAsync(subsidy);
            var deMinimisDuplicateTask = FindDeMinimisDuplicates(subsidy);

            var hashDuplicates = await hashDuplicateTask;
            var originalIdDuplicates = await originalIdDuplicateTask;
            var deMinimisDuplicates = await deMinimisDuplicateTask;

            var mergedDuplicates = hashDuplicates.ToList();
            
            mergedDuplicates = mergedDuplicates.Concat(
                originalIdDuplicates.Where(od => mergedDuplicates.Any(hd => hd.Id == od.Id) == false)).ToList();
            mergedDuplicates = mergedDuplicates.Concat(
                deMinimisDuplicates.Where(od => mergedDuplicates.Any(hd => hd.Id == od.Id) == false)).ToList();

            return mergedDuplicates;

        }

        private static async Task<List<Subsidy>> FindDuplicatesByHashAsync(Subsidy subsidy)
        {
            //duplicity podle hashe můžeme hledat jen u firem, u lidí by to slučovalo nesmyslně
            if (string.IsNullOrWhiteSpace(subsidy.Recipient.Ico))
                return [];
            
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
                return [];

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

                return res.Documents
                    .Where(d => $"{d.Metadata.FileName}_{d.Metadata.DataSource}" !=
                                $"{subsidy.Metadata.FileName}_{subsidy.Metadata.DataSource}")
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Cant load duplicate subsidies for subsidyId={subsidy.Id}");
                return [];
            }
        }
        
        public static async Task<List<Subsidy>> FindDeMinimisDuplicates(Subsidy subsidy)
        {
            //duplicity podle hashe můžeme hledat jen u firem, u lidí by to slučovalo nesmyslně
            if (string.IsNullOrWhiteSpace(subsidy.Recipient.Ico))
                return [];
            
            if(subsidy.Metadata.DataSource.ToLower() != "deminimis")
                return [];
            
            var queryForDuplicates = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Field("metadata.dataSource.keyword").Value("IsRed")),
                        m => m.Term(t => t.Field(f => f.DuplaHash).Value(subsidy.DuplaHash))
                    )
                );

            try
            {
                var res = await SubsidyClient.SearchAsync<Subsidy>(s => s
                    .Size(9000)
                    .Query(q => queryForDuplicates)
                );

                if (!res.IsValid) // try again
                {
                    await Task.Delay(500);
                    res = await SubsidyClient.SearchAsync<Subsidy>(s => s
                        .Size(9000)
                        .Query(q => queryForDuplicates)
                    );
                }
                
                if(res.Documents.Count == 0)
                    return [];

                //check if project code is in project name
                var returnList = res.Documents.ToList();
                returnList = returnList
                    .Where(
                        s => subsidy.ProjectName.Contains(s.ProjectCode, StringComparison.InvariantCultureIgnoreCase))
                    .ToList();
                
                if(!returnList.Any())
                    return [];
                
                returnList.Add(subsidy);
                return returnList;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Cant load duplicate subsidies for subsidyId={subsidy.Id}");
                return [];
            }
        }

        public static async Task<List<Subsidy>> FindDuplicatesByOriginalIdAsync(Subsidy subsidy)
        {
            if (string.IsNullOrWhiteSpace(subsidy.OriginalId))
                return [];

            // Build the main query
            var query = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.OriginalId, subsidy.OriginalId)
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
                return [];
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

        public static async Task<bool> ExistsAsync(string idDotace)
        {
            if (idDotace == null) throw new ArgumentNullException(nameof(idDotace));

            var response = await SubsidyClient.DocumentExistsAsync<Subsidy>(idDotace);

            return response.Exists;
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