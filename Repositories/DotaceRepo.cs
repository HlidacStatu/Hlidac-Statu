using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using Serilog;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories.Searching;

namespace HlidacStatu.Repositories
{
    public static partial class DotaceRepo
    {
        private static readonly ILogger Logger = Log.ForContext(typeof(DotaceRepo));

        private static ElasticClient DotaceClient => Manager.GetESClient_Dotace();

        public const int LastCompleteYear = 2024;


        public static async Task SaveAsync(Dotace dotace, bool forceRewriteHints = false)
        {
            Logger.Debug(
                $"Saving Dotace {dotace.Id} from {dotace.PrimaryDataSource}");
            if (dotace is null) throw new ArgumentNullException(nameof(dotace));
            
            //recalc hints
            try
            {
                await UpdateHintsAsync(dotace, false);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"Error while updating Hints for dotace {dotace.Id}");
            }

            if (!forceRewriteHints)
            {
                try
                {
                    var oldDotace = await GetAsync(dotace.Id);
                    if (oldDotace?.Hints is not null)
                    {
                        dotace.Hints = oldDotace.Hints;
                    }
                }
                catch (Exception e)
                {
                    Logger.Warning(e, $"Failed to load old Dotace {dotace.Id} to preserve hints");
                }
            }

            FixSomeHints(dotace);

            dotace.ModifiedDate = DateTime.Now;
            var res = await DotaceClient.IndexAsync(dotace, o => o.Id(dotace.Id));

            if (!res.IsValid)
            {
                Logger.Error($"Failed to save Dotace for {dotace.Id}. {res.OriginalException.Message}");
                throw new ApplicationException(res.ServerError?.ToString());
            }

            Logger.Debug(
                $"Dotace {dotace.Id} from {dotace.PrimaryDataSource} saved");
        }

        public static async Task<bool> UpdateHintsAsync(Dotace item, bool forceRewriteHints)
        {
            bool changed = false;
            if (item.Hints?.Category1 == null || forceRewriteHints == true)
            {
                List<Dotace.Hint.Category> cats = new();
                cats.AddRange(Dotace.Hint.ToCalculatedCategory(item));
                item.Hints.SetCategories(cats);
                changed = true;
            }


            Firma f = Firmy.Get(item.Recipient.Ico);
            DateTime dotaceDate = new DateTime(item.ApprovedYear ?? 1970, 1, 1);

            if (f.Valid && (item.Hints.RecipientStatus == -1 || forceRewriteHints))
            {
                item.Hints.RecipientTypSubjektu = (int)f.TypSubjektu;
                item.Hints.RecipientStatus = f.Status ?? 1;
                item.Hints.RecipientPolitickyAngazovanySubjekt = (int)HintSmlouva.PolitickaAngazovanostTyp.Neni;
                if (f.IsSponzorBefore(dotaceDate))
                    item.Hints.RecipientPolitickyAngazovanySubjekt =
                        (int)HintSmlouva.PolitickaAngazovanostTyp.PrimoSubjekt;
                else if (await f.MaVazbyNaPolitikyPredAsync(dotaceDate))
                    item.Hints.RecipientPolitickyAngazovanySubjekt =
                        (int)HintSmlouva.PolitickaAngazovanostTyp.AngazovanyMajitel;

                item.Hints.RecipientPocetLetOdZalozeni = 99;
                item.Hints.RecipientPocetLetOdZalozeni =
                    (dotaceDate.Year - (f.Datum_Zapisu_OR ?? new DateTime(1990, 1, 1)).Year);
                changed = true;
            }
            else
            {
                //item.Hints.RecipientTypSubjektu = (int)Firma.TypSubjektuEnum.Neznamy;
                item.Hints.RecipientStatus = 0;
                item.Hints.RecipientPolitickyAngazovanySubjekt = (int)HintSmlouva.PolitickaAngazovanostTyp.Neni;
                changed = true;
            }

            return changed;
        }


        public static async Task<Dotace> GetAsync(string idDotace)
        {
            if (idDotace == null) throw new ArgumentNullException(nameof(idDotace));

            var response = await DotaceClient.GetAsync<Dotace>(idDotace);

            return response.IsValid
                ? response.Source
                : null;
        }

        public static async Task<bool> ExistsAsync(string idDotace)
        {
            if (idDotace == null) throw new ArgumentNullException(nameof(idDotace));

            var response = await DotaceClient.DocumentExistsAsync<Dotace>(idDotace);

            return response.Exists;
        }

        /// <summary>
        /// Get all dotace. If query is null, then it matches all except hidden ones
        /// </summary>
        /// <param name="query"></param>
        /// <param name="scrollTimeout"></param>
        /// <param name="scrollSize"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async IAsyncEnumerable<Dotace> GetAllAsync(QueryContainer query,
            string scrollTimeout = "5m",
            int scrollSize = 300)
        {
            ISearchResponse<Dotace> initialResponse = null;
            if (query is null)
            {
                initialResponse = await DotaceClient.SearchAsync<Dotace>(scr => scr
                    .From(0)
                    .Take(scrollSize)
                    .MatchAll()
                    .Scroll(scrollTimeout));
            }
            else
            {
                initialResponse = await DotaceClient.SearchAsync<Dotace>(scr => scr
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
                ISearchResponse<Dotace> loopingResponse =
                    await DotaceClient.ScrollAsync<Dotace>(scrollTimeout, scrollid);
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

            _ = await DotaceClient.ClearScrollAsync(new ClearScrollRequest(scrollid));
        }

        public static IAsyncEnumerable<Dotace> GetDotaceForIcoAsync(string ico)
        {
            QueryContainer qc = new QueryContainerDescriptor<Dotace>()
                .Term(f => f.Recipient.Ico, ico);

            return GetAllAsync(qc);
        }

        public static async Task MergeAllSubsidiesToDotaceAsync(DateTime fromDate, bool rewriteAll = true)
        {
            int maxDegreeOfParallelism = 20; // Adjust this value as needed
            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            int processedCount = 0;

            QueryContainer originalsQuery;

            originalsQuery = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Metadata.IsHidden, false),
                        m => m.Term(t => t.Hints.IsOriginal, true),
                        m => m.DateRange(r => r
                            .Field(f => f.Metadata.ModifiedDate)
                            .GreaterThanOrEquals(fromDate)
                        )
                    )
                );

            var originalSubsidyIds = SubsidyRepo.SubsidyClient.SimpleGetAllIds(5, originalsQuery);
            int totalCount = originalSubsidyIds.Count;

            var tasks = originalSubsidyIds.Select(async originalSubsidyId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    Console.WriteLine(
                        $"Remaining to process: {totalCount - Interlocked.Increment(ref processedCount)}");

                    Dotace dotaceRecord = new Dotace();

                    var originalSubsidy = await SubsidyRepo.GetAsync(originalSubsidyId);

                    FixRecipientFromRawData(originalSubsidy);
                    dotaceRecord.UpdateFromSubsidy(originalSubsidy);

                    //update missing data from subsidy duplicates
                    foreach (var duplicateId in originalSubsidy.Hints.Duplicates)
                    {
                        if (dotaceRecord.SourceIds.Contains(duplicateId))
                            continue;

                        var duplicateSubsidy = await SubsidyRepo.GetAsync(duplicateId);
                        FixRecipientFromRawData(duplicateSubsidy);
                        dotaceRecord.UpdateFromSubsidy(duplicateSubsidy);
                    }

                    try
                    {
                        bool isNew = false;
                        bool isChanged = false;

                        if (rewriteAll == false)
                        {
                            //check if dotace exists
                            var dotaceOrig = await DotaceRepo.GetAsync(dotaceRecord.Id);
                            if (dotaceOrig == null)
                            {
                                isNew = true;
                            }
                            else
                            {
                                isChanged = IsDotaceChanged(dotaceOrig, dotaceRecord);
                            }
                        }

                        if (rewriteAll || isNew || isChanged)
                        {
                            await SaveAsync(dotaceRecord);

                            //add to statistics
                            RecalculateItem.DotaceOptions.ChangeEnum ce = isChanged
                                ? RecalculateItem.DotaceOptions.ChangeEnum.Update
                                : RecalculateItem.DotaceOptions.ChangeEnum.Insert;
                            AddToProcessingQueue(dotaceRecord, ce);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Failed to save dotace for {originalSubsidy.Id}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error processing ID {originalSubsidyId}: {e}");
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);
        }

        public static bool IsDotaceChanged(Dotace dotaceOriginal, Dotace dotaceNew)
        {
            if (dotaceOriginal.Id != dotaceNew.Id)
                return true;


            if (dotaceOriginal.ApprovedYear != dotaceNew.ApprovedYear
                || dotaceOriginal.SubsidyAmount != dotaceNew.SubsidyAmount
                || dotaceOriginal.PayedAmount != dotaceNew.PayedAmount
                || dotaceOriginal.ReturnedAmount != dotaceNew.ReturnedAmount
                || dotaceOriginal.Category != dotaceNew.Category
                || dotaceOriginal.ProjectCode != dotaceNew.ProjectCode
                || dotaceOriginal.ProjectName != dotaceNew.ProjectName
                || dotaceOriginal.ProjectDescription != dotaceNew.ProjectDescription
                || dotaceOriginal.ProgramCode != dotaceNew.ProgramCode
                || dotaceOriginal.ProgramName != dotaceNew.ProgramName
                || dotaceOriginal.SubsidyProvider != dotaceNew.SubsidyProvider
                || dotaceOriginal.SubsidyProviderIco != dotaceNew.SubsidyProviderIco
                || dotaceOriginal.Recipient.Ico != dotaceNew.Recipient.Ico
                || dotaceOriginal.Recipient.Name != dotaceNew.Recipient.Name
                || dotaceOriginal.Recipient.HlidacName != dotaceNew.Recipient.HlidacName
                || dotaceOriginal.Recipient.YearOfBirth != dotaceNew.Recipient.YearOfBirth
                || dotaceOriginal.Recipient.Obec != dotaceNew.Recipient.Obec
                || dotaceOriginal.Recipient.Okres != dotaceNew.Recipient.Okres
                || dotaceOriginal.Recipient.PSC != dotaceNew.Recipient.PSC
                || dotaceOriginal.Recipient.HlidacNameId != dotaceNew.Recipient.HlidacNameId
                || dotaceOriginal.Hints.IsOriginal != dotaceNew.Hints.IsOriginal
                || dotaceOriginal.Hints.OriginalSubsidyId != dotaceNew.Hints.OriginalSubsidyId
                || dotaceOriginal.PrimaryDataSource != dotaceNew.PrimaryDataSource
               )
            {
                return true;
            }

            if (!dotaceOriginal.SourceIds.SetEquals(dotaceNew.SourceIds))
            {
                return true;
            }

            return false;
        }


        public static async Task RemoveMissingSubsidiesFromDotaceAsync()
        {
            int maxDegreeOfParallelism = 10; // Adjust this value as needed
            int processedCount = 0;

            var subsidyQuery = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Metadata.IsHidden, false),
                        m => m.Term(t => t.Hints.IsOriginal, true)
                    )
                );

            var existingSubsidyIds = SubsidyRepo.SubsidyClient.SimpleGetAllIds(5, subsidyQuery);
            var dotaceQuery = new QueryContainerDescriptor<Dotace>().MatchAll();
            var dotaceIdsToRemove = DotaceClient.SimpleGetAllIds(5, dotaceQuery).ToHashSet();

            dotaceIdsToRemove.ExceptWith(existingSubsidyIds);
            int totalCount = dotaceIdsToRemove.Count;

            await Parallel.ForEachAsync(dotaceIdsToRemove,
                new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                async (dotaceToRemoveId, cancellationToken) =>
                {
                    try
                    {
                        Console.WriteLine(
                            $"Remaining to remove: {totalCount - Interlocked.Increment(ref processedCount)}");

                        try
                        {
                            var dotaceToRemove = await DotaceRepo.GetAsync(dotaceToRemoveId);

                            AddToProcessingQueue(dotaceToRemove, RecalculateItem.DotaceOptions.ChangeEnum.Delete);
                            await DotaceClient.DeleteAsync<Dotace>(dotaceToRemoveId, ct: cancellationToken);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e, $"Failed to remove dotace for {dotaceToRemoveId}");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $"Error removing ID {dotaceToRemoveId}");
                    }
                }
            );
        }

        private static void AddToProcessingQueue(Dotace dotace, RecalculateItem.DotaceOptions.ChangeEnum changeType)
        {
            var dotaceOptions = new RecalculateItem.DotaceOptions()
            {
                ForceRecalculate = false,
                Version = 1,
                PoskytovatelIco = dotace.SubsidyProviderIco,
                PrijemceIco = dotace.Recipient.Ico,
                Change = changeType
            };

            var recalculateItem = new RecalculateItem(dotace, dotaceOptions, "AddToProcessingQueueAsync");
            try
            {
                RecalculateItemRepo.AddToProcessingQueue(recalculateItem);
            }
            catch (Exception e)
            {
                Logger.Error(e, $"AddToProcessingQueue failed to add dotaceId={dotace.Id} item from RecipientIco[{dotace.Recipient.Ico}] to processing queue;");
            }
        }

        private static void FixSomeHints(Dotace dotace)
        {
            if (dotace.PrimaryDataSource.ToLower() == "deminimis")
            {
                dotace.Hints.Category1 = new Dotace.Hint.Category()
                {
                    Created = DateTime.Now,
                    Probability = 1m,
                    TypeValue = (int)Dotace.Hint.CalculatedCategories.MaleStredniPodniky
                };
            }

            if (dotace.PrimaryDataSource.ToLower() == "covid")
            {
                dotace.Hints.Category1 = new Dotace.Hint.Category()
                {
                    Created = DateTime.Now,
                    Probability = 1m,
                    TypeValue = (int)Dotace.Hint.CalculatedCategories.Covid
                };
            }
        }

        public static void FixRecipientFromRawData(Subsidy subsidy)
        {
            try
            {
                //fix dotinfo
                if (subsidy.Metadata.DataSource.ToLower() == "dotinfo")
                {
                    if (subsidy.RawData.FirstOrDefault() is Dictionary<string, object> dict &&
                        dict.TryGetValue("ucastnik", out var ucastnikObj) &&
                        ucastnikObj is Dictionary<string, object> ucastnikDict)
                    {
                        if (string.IsNullOrWhiteSpace(subsidy.Recipient.Name) &&
                            ucastnikDict.TryGetValue("prijemce dotace jmeno", out var nameObj) &&
                            nameObj is string name)
                        {
                            subsidy.Recipient.Name = name;
                        }

                        if (subsidy.Recipient.YearOfBirth is null &&
                            ucastnikDict.TryGetValue("rc ucastnika", out var rcObj) && rcObj is string rc)
                        {
                            //take first two letters and make a year from them
                            var year = int.Parse(rc.Substring(0, 2));

                            subsidy.Recipient.YearOfBirth = year < 40 ? 2000 + year : 1900 + year;
                        }
                    }
                }

                //fix cedr
                if (subsidy.Metadata.DataSource.ToLower() == "cedr")
                {
                    if (string.IsNullOrWhiteSpace(subsidy.Recipient.Name))
                    {
                        if (subsidy.RawData.FirstOrDefault() is Dictionary<string, object> dict &&
                            dict.TryGetValue("prijemcejmeno", out var nameObj) && nameObj is string name)
                        {
                            subsidy.Recipient.Name = name;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Warning(e, $"Failed to unwrap data from raw with subsidy{subsidy.Id}");
            }
        }

        public static async Task<List<string>> GetDistinctProgramsAsync()
        {
            var scrollTimeout = "2m";
            var batchSize = 5000;
            var uniqueProgramNames = new HashSet<string>();

            var searchResponse = await DotaceRepo.DotaceClient.SearchAsync<Dotace>(s => s
                    .Size(batchSize)
                    .Scroll(scrollTimeout)
                    .Source(src => src.Includes(f => f.Field("programName"))) // Fetch only needed field
            );

            var scrollId = searchResponse.ScrollId;

            if (searchResponse.Documents != null)
            {
                foreach (var doc in searchResponse.Documents)
                {
                    if (!string.IsNullOrEmpty(doc.ProgramName))
                        uniqueProgramNames.Add(doc.ProgramName);
                }
            }

            while (!string.IsNullOrEmpty(scrollId) && searchResponse.Documents.Any())
            {
                searchResponse = await DotaceRepo.DotaceClient.ScrollAsync<Dotace>(scrollTimeout, scrollId);
                scrollId = searchResponse.ScrollId;

                if (searchResponse.Documents != null)
                {
                    foreach (var doc in searchResponse.Documents)
                    {
                        if (!string.IsNullOrEmpty(doc.ProgramName))
                            uniqueProgramNames.Add(doc.ProgramName);
                    }
                }
            }

            await DotaceRepo.DotaceClient.ClearScrollAsync(cs => cs.ScrollId(scrollId));

            return uniqueProgramNames.ToList();
        }

        public static async Task DeleteAsync(string dotaceId)
        {
            Logger.Debug($"Removing dotace {dotaceId}.");
            try
            {
                await DotaceClient.DeleteAsync<Dotace>(dotaceId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Cant delete dotace {dotaceId}");
                throw;
            }
        }
        
        /// <summary>
        /// Pokud známe id výzvy, pak vracíme výzvu.
        /// Pokud neznáme id výzvy, pak ji zkusíme dohledat:
        /// a) pokud výzvu dohledáme, updatuje se objekt Subsidy a Dotace
        /// b) pokud výzvu nenajdeme, tak se nic neděje
        /// </summary>
        public static async Task FillDotacniVyzvaInDotaceAsync()
        {
            int maxDegreeOfParallelism = 20; // Adjust this value as needed
            var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            int processedCount = 0;

            var dotaceQuery = new QueryContainerDescriptor<Dotace>().MatchAll();
            var allDotaceIds = DotaceRepo.DotaceClient.SimpleGetAllIds(5, dotaceQuery);
            
            int totalCount = allDotaceIds.Count;

            var tasks = allDotaceIds.Select(async dotaceId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    Console.WriteLine(
                        $"Remaining to process: {totalCount - Interlocked.Increment(ref processedCount)}");

                    var dotace = await DotaceRepo.GetAsync(dotaceId);

                    foreach (var dotaceSourceId in dotace.SourceIds)
                    {
                        var subsidy = await SubsidyRepo.GetAsync(dotaceSourceId);
                        var dotacniVyzva = await SubsidyRepo.GetVyzvaForSubsidyAsync(subsidy);

                        if (dotacniVyzva is not null)
                        {
                            subsidy.IdDotacniVyzvy = dotacniVyzva.Id;
                            dotace.IdDotacniVyzvy = dotacniVyzva.Id;

                            await SubsidyRepo.SaveAsync(subsidy, true);
                            await DotaceRepo.SaveAsync(dotace, forceRewriteHints: false);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error when updating dotacniVyzva for dotace ID {dotaceId}: {e}");
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);
        }
    }
}