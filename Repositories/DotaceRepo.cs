using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static readonly ElasticClient DotaceClient = Manager.GetESClient_DotaceAsync()
            .ConfigureAwait(false).GetAwaiter().GetResult();
        
        

        public static async Task SaveAsync(Dotace dotace)
        {
            Logger.Debug(
                $"Saving Dotace {dotace.Id} from {dotace.PrimaryDataSource}");
            if (dotace is null) throw new ArgumentNullException(nameof(dotace));
            

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

        public static bool UpdateHints(Dotace item, bool forceRewriteHints)
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
                item.Hints.RecipientStatus = (int)f.Status;
                item.Hints.RecipientPolitickyAngazovanySubjekt = (int)HintSmlouva.PolitickaAngazovanostTyp.Neni;
                if (f.IsSponzorBefore(dotaceDate))
                    item.Hints.RecipientPolitickyAngazovanySubjekt =
                        (int)HintSmlouva.PolitickaAngazovanostTyp.PrimoSubjekt;
                else if (f.MaVazbyNaPolitikyPred(dotaceDate))
                    item.Hints.RecipientPolitickyAngazovanySubjekt =
                        (int)HintSmlouva.PolitickaAngazovanostTyp.AngazovanyMajitel;

                item.Hints.RecipientPocetLetOdZalozeni = 99;
                item.Hints.RecipientPocetLetOdZalozeni = (dotaceDate.Year - (f.Datum_Zapisu_OR ?? new DateTime(1990, 1, 1)).Year);
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
        /// Get all subsidies. If query is null, then it matches all except hidden ones
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
        
        public static async Task MergeSubsidiesToDotaceAsync()
        {
            var originalsQuery = new QueryContainerDescriptor<Subsidy>()
                .Bool(b => b
                    .Must(
                        m => m.Term(t => t.Metadata.IsHidden, false),
                        m => m.Term(t => t.Hints.IsOriginal, true)
                    )
                );
            
            var idsToMerge = SubsidyRepo.SubsidyClient.SimpleGetAllIds(5, originalsQuery);

            foreach (var idToMerge in idsToMerge)
            {
                var originalSubsidy = await SubsidyRepo.GetAsync(idToMerge);
                Dotace dotaceRecord = new Dotace()
                {

                };
                
                foreach (var duplicateId in originalSubsidy.Hints.Duplicates)
                {
                    var duplicateSubsidy = await SubsidyRepo.GetAsync(duplicateId);
                    UpdateUnsetProperties(originalSubsidy, duplicateSubsidy);
                }

                originalSubsidy.RawData = new();
                
            }
            
        }
    }
}