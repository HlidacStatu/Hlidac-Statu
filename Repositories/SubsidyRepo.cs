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

        public static ElasticClient SubsidyClient => Manager.GetESClient_Subsidy();

        public static readonly string[] SubsidyDuplicityOrdering =
            ["IsRed", "Cedr", "Eufondy", "Státní zemědělský intervenční fond"];

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
            Logger.Debug($"Waiting to acquire semaphore for subsidy {subsidy.Id}");
            try
            {
                Logger.Debug($"Semaphore acquired for subsidy {subsidy.Id}");
                await SaveThreadUnsafeAsync(subsidy, shouldRewrite);
            }
            catch (Exception e)
            {
                Logger.Error(e,
                    $"Failed to save subsidy with id=[{subsidy.Id}] from: {subsidy.Metadata.DataSource} - {subsidy.Metadata.FileName} - {subsidy.Metadata.RecordNumber}");
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

        public static async Task<List<string>> FindAndSetDuplicatesThreadUnsafeAsync(Subsidy baseSubsidy)
        {
            var allSubsidies = await FindDuplicatesAsync(baseSubsidy);
            //no duplicates exist 
            if (allSubsidies == null || !allSubsidies.Any())
            {
                return [];
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
                if (allSubsidies.All(m => m.Id != id))
                {
                    var s = await GetAsync(id);
                    if (s != null)
                    {
                        allSubsidies.Add(s);
                    }
                }
            }

            var ids = await ConsolidatedAndSetDuplicatesThreadUnsafeAsync(allSubsidies);
            return ids;
        }

        public static async Task<List<string>> ConsolidatedAndSetDuplicatesThreadUnsafeAsync(params Subsidy[] subsidies)
        {
            return await ConsolidatedAndSetDuplicatesThreadUnsafeAsync(new List<Subsidy>(subsidies));
        }

        public static async Task<List<string>> ConsolidatedAndSetDuplicatesThreadUnsafeAsync(List<Subsidy> subsidies)
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
            return subsidies.Select(s => s.Id).ToList();
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

        private static void SetDuplicate(Subsidy subsidy, List<string> duplicates, List<string> hiddenDuplicates,
            string originalSubsidyId)
        {
            if (string.IsNullOrEmpty(originalSubsidyId))
                return;

            if (originalSubsidyId == subsidy.Id)
            {
                //set as original
                subsidy.Hints.IsOriginal = true;
                subsidy.Hints.OriginalSubsidyId = null;
                subsidy.Hints.DuplicateCalculated = DateTime.Now;
                subsidy.Hints.Duplicates = duplicates.Where(d => d != subsidy.Id).ToList();
                subsidy.Hints.HiddenDuplicates = hiddenDuplicates.Where(d => d != subsidy.Id).ToList();
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
            var euFondyDuplicateTask = FindDuplicatesByEuFondyProjectCode(subsidy);
            var isRedDeMinimisDuplicateTask = FindIsRedDeMinimisDuplicates(subsidy);

            var hashDuplicates = await hashDuplicateTask;
            var originalIdDuplicates = await originalIdDuplicateTask;
            var deMinimisDuplicates = await deMinimisDuplicateTask;
            var euFondyDuplicates = await euFondyDuplicateTask;
            var isRedDeMinimisDuplicates = await isRedDeMinimisDuplicateTask;

            var mergedDuplicates = hashDuplicates.ToList();

            mergedDuplicates = mergedDuplicates.Concat(
                originalIdDuplicates.Where(od => mergedDuplicates.Any(hd => hd.Id == od.Id) == false)).ToList();
            mergedDuplicates = mergedDuplicates.Concat(
                deMinimisDuplicates.Where(od => mergedDuplicates.Any(hd => hd.Id == od.Id) == false)).ToList();
            mergedDuplicates = mergedDuplicates.Concat(
                euFondyDuplicates.Where(od => mergedDuplicates.Any(hd => hd.Id == od.Id) == false)).ToList();
            mergedDuplicates = mergedDuplicates.Concat(
                isRedDeMinimisDuplicates.Where(od => mergedDuplicates.Any(hd => hd.Id == od.Id) == false)).ToList();
            
            
            mergedDuplicates = FixDuplicateExceptions(subsidy, mergedDuplicates);
            

            return mergedDuplicates;
        }

        private static List<Subsidy> FixDuplicateExceptions(Subsidy subsidy, List<Subsidy> mergedDuplicates)
        {
            if (subsidy.ProjectCode == "MV-25297-33/PO-2009")
            {
                return mergedDuplicates.Where(md => md.ProjectName == subsidy.ProjectName).ToList();
            }

            return mergedDuplicates;
        }

        public static async Task<List<Subsidy>> FindDuplicatesByHashAsync(Subsidy subsidy)
        {
            try
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
                    var res = await QuerySearchAsync(query);

                    return res
                        //Dotace není v rámci stejného zdroje
                        .Where(d => d.Metadata.DataSource != subsidy.Metadata.DataSource)
                        //remove those where we try pair IsRed with Cedr and ProjectCode differs...
                        .Where(d => !(subsidy.Metadata.DataSource == "IsRed" && d.Metadata.DataSource == "Cedr" && d.ProjectCodeHash != subsidy.ProjectCodeHash ))
                        //remove those where we try pair Cedr with IsRed and ProjectCode differs...
                        .Where(d => !(subsidy.Metadata.DataSource == "Cedr" && d.Metadata.DataSource == "IsRed" && d.ProjectCodeHash != subsidy.ProjectCodeHash ))
                        //remove multiples from DotInfo cross Cedr or IsRed
                        .Where(d => !ShouldRemoveDotInfoEntry(subsidy, d))
                        .ToList();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Cant load duplicate subsidies for subsidyId={subsidy.Id}");
                    return [];
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Problem when finding duplicates by hash. Id={subsidy.Id}");
                return [];
            }
        }

        private static bool ShouldRemoveDotInfoEntry(Subsidy originalSubsidy, Subsidy checkedSubsidy)
        {
            var originalSource = originalSubsidy.Metadata.DataSource;
            var checkedSource = checkedSubsidy.Metadata.DataSource;

            bool isOriginalRedOrCedr = originalSource == "IsRed" || originalSource == "Cedr";
            bool isCheckedRedOrCedr = checkedSource == "IsRed" || checkedSource == "Cedr";
    
            bool isOriginalDotInfo = originalSource == "DotInfo";
            bool isCheckedDotInfo = checkedSource == "DotInfo";
            
            if(!((isOriginalRedOrCedr && isCheckedDotInfo) 
                 || (isOriginalDotInfo && isCheckedRedOrCedr)))
                return false;
            
            Subsidy dotInfoSubsidy = null;
            Subsidy isRedSubsidy = null;
            
            if (isOriginalDotInfo)
            {
                dotInfoSubsidy = originalSubsidy;
                isRedSubsidy = checkedSubsidy;
            }
            else
            {
                dotInfoSubsidy = checkedSubsidy;
                isRedSubsidy = originalSubsidy;
            }
            
            if (isRedSubsidy.ProjectCode.ToLower().Trim() == dotInfoSubsidy.ProjectCode.ToLower().Trim())
                return false;

            //Check if the project number is in another field in dotinfo
            if (dotInfoSubsidy.RawData.Any(item =>
                    item is Dictionary<string, object> dict &&
                    dict.TryGetValue("dotace", out var dotaceObj) &&
                    dotaceObj is Dictionary<string, object> dotaceDict &&
                    dotaceDict.TryGetValue("evidencni cislo dotace", out var evidencniCisloObj) &&
                    evidencniCisloObj?.ToString()?.ToLower().Trim() == isRedSubsidy.ProjectCode.ToLower().Trim()))
                return false;
            
            //TBC....
            return true;
        }

        private static string[] EufondyDataSources = new[]
        {
            "eufondy", "cedr", "isred", "dotinfo"
        };

        private static async Task<List<Subsidy>> FindDuplicatesByEuFondyProjectCode(Subsidy subsidy)
        {
            try
            {
                //duplicity podle hashe můžeme hledat jen u firem, u lidí by to slučovalo nesmyslně
                if (string.IsNullOrWhiteSpace(subsidy.Recipient.Ico))
                    return [];

                if (string.IsNullOrWhiteSpace(subsidy.ProjectCode))
                    return [];

                if (!subsidy.ProjectCode.ToLower()
                        .StartsWith("cz.")) //only EU fondy project codes like "CZ.03.2.60/0.0/0.0/15_022/0001007"
                    return [];

                if (!EufondyDataSources.Contains(subsidy.Metadata.DataSource.ToLower())) //only EU fondy data sources
                    return [];

                List<Subsidy> results = new();
                // Build the main query
                var query = new QueryContainerDescriptor<Subsidy>()
                    .Bool(b => b
                        .Must(
                            m => m.Term(t => t.Recipient.Ico, subsidy.Recipient.Ico),
                            m => m.Term(t => t.ProjectCode.Suffix("keyword"), subsidy.ProjectCode)
                        )
                    );

                try
                {
                    var res = await QuerySearchAsync(query);

                    var firstRes = res
                        .Where(d => $"{d.Metadata.FileName}_{d.Metadata.DataSource}" !=
                                    $"{subsidy.Metadata.FileName}_{subsidy.Metadata.DataSource}")
                        .Where(d => EufondyDataSources.Contains(d.Metadata.DataSource
                            .ToLower())) //only EU fondy data sources
                        .ToList();

                    results.AddRange(firstRes);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Cant load eufondy duplicates for subsidyId={subsidy.Id}");
                    return results;
                }

                // Build the de minimis query
                var deMinimisQuery = new QueryContainerDescriptor<Subsidy>()
                    .Bool(b => b
                        .Must(
                            m => m.Term(t => t.Recipient.Ico, subsidy.Recipient.Ico),
                            m => m.Term(t => t.Metadata.DataSource.Suffix("keyword"), "DeMinimis"),
                            m => m.MatchPhrase(mp => mp
                                .Field(f => f.ProjectName)
                                .Query(subsidy.ProjectCode)
                            )
                        )
                    );

                try
                {
                    var deMinimisRes = await QuerySearchAsync(deMinimisQuery);

                    var secondRes = deMinimisRes
                        .Where(d => $"{d.Metadata.FileName}_{d.Metadata.DataSource}" !=
                                    $"{subsidy.Metadata.FileName}_{subsidy.Metadata.DataSource}")
                        .Where(d => EufondyDataSources.Contains(d.Metadata.DataSource
                            .ToLower())) //only EU fondy data sources
                        .ToList();

                    results.AddRange(secondRes);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Cant load eufony duplicates from deminimis for subsidyId={subsidy.Id}");
                    return results;
                }

                return results;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Problem when finding duplicates by EuFondyProjectCode. Id={subsidy.Id}");
                return [];
            }
        }

        public static async Task<List<Subsidy>> FindDeMinimisDuplicates(Subsidy subsidy)
        {
            try
            {
                //duplicity podle hashe můžeme hledat jen u firem, u lidí by to slučovalo nesmyslně
                if (string.IsNullOrWhiteSpace(subsidy.Recipient.Ico))
                    return [];

                if (subsidy.Metadata.DataSource.ToLower() != "deminimis")
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
                    var returnList = await QuerySearchAsync(queryForDuplicates);
                    
                    returnList = returnList
                        .Where(
                            s => subsidy.ProjectName.Contains(s.ProjectCode,
                                StringComparison.InvariantCultureIgnoreCase))
                        .ToList();

                    if (!returnList.Any())
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
            catch (Exception e)
            {
                Logger.Error(e, $"Problem when finding duplicates in DeMinimis. Id={subsidy.Id}");
                return [];
            }
        }

        public static async Task<List<Subsidy>> FindIsRedDeMinimisDuplicates(Subsidy subsidy)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subsidy.Recipient.Ico)
                    || subsidy.ApprovedYear is null
                    || subsidy.Metadata.DataSource.ToLower() != "isred"
                    || string.IsNullOrWhiteSpace(subsidy.ProjectName)
                    || subsidy.ProjectName?.Length < 20) // this is fail safe, so not some short nonsense like "2022" marks many results
                    return [];
                
                var deMinimisQuery = new QueryContainerDescriptor<Subsidy>()
                    .Bool(b => b
                        .Must(
                            m => m.Term(t => t.Metadata.DataSource.Suffix("keyword"), "DeMinimis"),
                            m => m.Term(t => t.Recipient.Ico, subsidy.Recipient.Ico),
                            m => m.Term(t => t.ApprovedYear, subsidy.ApprovedYear),
                            m => m.Term(t => t.AssumedAmount, subsidy.AssumedAmount),
                            m => m.Term(t => t.SubsidyProviderIco, subsidy.SubsidyProviderIco),
                            m => m.QueryString(q => q.DefaultField(f => f.ProjectName).Query($"\"{subsidy.ProjectName.Replace("\"", "\\\"")}\""))
                        )
                    );

                try
                {
                    var deMinimisSubsidies = await QuerySearchAsync(deMinimisQuery);
                    if (deMinimisSubsidies is not null
                        && deMinimisSubsidies.Count > 1)
                    {
                        // too many results - there should be only one
                        return [];
                    }


                    return deMinimisSubsidies;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Cant load duplicate subsidies from DeMinimis for subsidyId={subsidy.Id}");
                    return [];
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Problem when finding duplicates between IsRed and DeMinimis. Id={subsidy.Id}");
                return [];
            }
        }
        
        public static async Task<List<Subsidy>> FindDuplicatesByOriginalIdAsync(Subsidy subsidy)
        {
            try
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
                    var res = await QuerySearchAsync(query);

                    return res;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, $"Cant load duplicate subsidies for subsidyId={subsidy.Id}");
                    return [];
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Problem when finding duplicates by Original ID. Id={subsidy.Id}");
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
        
        public static async Task<List<Subsidy>> QuerySearchAsync(QueryContainer query)
        {
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
                Logger.Error(ex, $"Cant load subsidies for query");
                throw;
            }
        }
        
        public static async Task<DotacniVyzva> GetVyzvaForSubsidyAsync(Subsidy subsidy)
        {
            if (subsidy == null) throw new ArgumentNullException(nameof(subsidy));

            //if we know id, then return it immediatelly
            if(! string.IsNullOrWhiteSpace(subsidy.IdDotacniVyzvy))
                return await DotacniVyzvaRepo.GetAsync(subsidy.IdDotacniVyzvy);

            if (subsidy.Metadata.DataSource.Equals("Eufondy", StringComparison.InvariantCultureIgnoreCase))
            {
                foreach (object rawData in subsidy.RawData)
                {
                    if (rawData is Dictionary<string, object> dict 
                        && dict.TryGetValue("idVyzva", out var idVyzvyObj)
                        && idVyzvyObj is string idVyzvy)
                    {
                        
                        var vyzvaResponse = await DotacniVyzvaRepo.DotacniVyzvaClient.GetAsync<DotacniVyzva>(idVyzvy);
                        
                        if(vyzvaResponse.IsValid) 
                            return vyzvaResponse.Source;
                    }
                }
            }
            
            //No vyzva found
            return null;
        }
    }
}