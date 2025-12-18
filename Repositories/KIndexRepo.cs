using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities.KIndex;
using Nest;
using Serilog;
using Consts = HlidacStatu.Entities.KIndex.Consts;

namespace HlidacStatu.Repositories;

public static class KIndexRepo
{
    private static readonly ILogger _logger = Log.ForContext(typeof(KIndexRepo));
    
    
    public static async Task<int[]> GetAvailableCalculationYearsAsync()
    {
        if (Consts.XAvailableCalculationYears is null || !Consts.XAvailableCalculationYears.Any())
        {
            var maxYear = (await Analysis.KorupcniRiziko.Statistics.GetKindexStatTotalAsync()).Max(m => m.Rok);
            Consts.XAvailableCalculationYears = Consts.ToCalculationYears
                .Where(r => r <= maxYear )
                .ToArray(); 
        }

        return Consts.XAvailableCalculationYears;

    }
    
    /// <summary>
    /// Checks if year is within the range (CalculationYears). 
    /// If null or later than max then sets it to the max year from the range.
    /// If earlier than the earliest year from range then sets it to the earliest one.
    /// </summary>
    /// <param name="year"></param>
    /// <returns></returns>
    public static async Task<int> FixKindexYearAsync(int? year)
    {
        var availableCalculationYears = await GetAvailableCalculationYearsAsync();
        if (year < availableCalculationYears.Min())
            return availableCalculationYears.Min();
        if (year is null || year >= availableCalculationYears.Max())
            return availableCalculationYears.Max();

        return year.Value;
    }
    
    
    
    public static async Task<KIndexData> EmptyAsync(string ico, string jmeno = null)
    {
        KIndexData kidx = new KIndexData();
        kidx.Ico = ico;
        kidx.Jmeno = jmeno ?? Firmy.GetJmeno(ico);
        kidx.roky = (await KIndexRepo.GetAvailableCalculationYearsAsync())
            .Select(rok => KIndexData.Annual.Empty(rok))
            .ToList();
        return kidx;
    }
    
    public static async Task SaveAsync(this KIndexData kIndexData, string comment, bool useTempDb = false)
    {
        await CreateBackupAsync(comment, kIndexData.Ico, useTempDb);

        //calculate fields before saving
        kIndexData.LastSaved = DateTime.Now;
        var client = Manager.GetESClient_KIndex();
        if (useTempDb)
            client = Manager.GetESClient_KIndexTemp();

        var res = await client.IndexAsync<KIndexData>(kIndexData, o => o.Id(kIndexData.Ico)); //druhy parametr musi byt pole, ktere je unikatni
        if (!res.IsValid)
        {
            throw new ApplicationException(res.ServerError?.ToString());
        }
    }
    
    public static async Task<IOrderedEnumerable<Backup>> GetPreviousVersionsAsync(this KIndexData kIndexData, bool futureData = false)
    {
        ElasticClient client = Manager.GetESClient_KIndexBackup();
        if (futureData)
            client = Manager.GetESClient_KIndexBackupTemp();

        ISearchResponse<Backup> searchResults = null;
        try
        {
            searchResults = await client.SearchAsync<Backup>(s =>
                    s.Query(q => q.Term(f => f.KIndex.Ico, kIndexData.Ico)));

            if (searchResults.IsValid && searchResults.Hits.Count > 0)
            {
                var hits = searchResults.Hits.Select(h => h.Source).OrderByDescending(s => s.Created);
                return hits;
            }
        }
        catch (Exception e)
        {
            string origQuery = $"ico:{kIndexData.Ico};";
            AuditRepo.Add(Audit.Operations.Search, "", "", "KindexFeedback", "error", origQuery, null);
            if (searchResults != null && searchResults.ServerError != null)
            {
                Manager.LogQueryError<Backup>(searchResults,
                    $"Exception for {origQuery}",
                    ex: e);
            }
            else
            {
                _logger.Error(e, origQuery);
            }
        }

        return Enumerable.Empty<Backup>().OrderBy(x => 1);

    }

        public static async Task<Backup> GetPreviousVersionAsync(string id)
        {
            ElasticClient client = Manager.GetESClient_KIndexBackup();
            if (!string.IsNullOrEmpty(Devmasters.Config.GetWebConfigValue("UseKindexTemp")))
                client = Manager.GetESClient_KIndexBackupTemp();

            try
            {
                var searchResult = await client.GetAsync<Backup>(id);

                if (searchResult.IsValid)
                {
                    return searchResult.Source;
                }
            }
            catch (Exception e)
            {
                string origQuery = $"id:{id};";
                AuditRepo.Add(Audit.Operations.Search, "", "", "KindexFeedback", "error", origQuery, null);
                _logger.Error(e, origQuery);
            }

            return null;
        }
    
    public static async Task<KIndexData> GetDirectAsync(string ico, bool useTempDb)
    {
        if (Consts.KIndexExceptions.Contains(ico) && useTempDb == false)
            return null;

        var client = Manager.GetESClient_KIndex();
        if (useTempDb)
            client = Manager.GetESClient_KIndexTemp();


        var res = await client.GetAsync<KIndexData>(ico);
        if (res.Found == false)
            return null;
        else if (!res.IsValid)
        {
            throw new ApplicationException(res.ServerError?.ToString());
        }
        else
        {
            KIndexData f = res.Source;
            //fill Annual
            foreach (var r in f.roky)
            {
                if (r != null)
                    r.Ico = ico;
            }
            return f;
        }
    }
    
    
    //Backup
    public static async Task CreateBackupAsync(string comment, KIndexData kidx, bool useTempDb)
    {

        if (kidx == null)
            return;
        Backup b = new Backup();
        //calculate fields before saving
        b.Created = DateTime.Now;
        b.Id = $"{kidx.Ico}_{b.Created:s}";
        b.Comment = comment;
        b.KIndex = kidx;
        var client = Manager.GetESClient_KIndexBackup();
        if (useTempDb)
            client = Manager.GetESClient_KIndexBackupTemp();

        var res = await client.IndexAsync<Backup>(b, o => o.Id(b.Id)); //druhy parametr musi byt pole, ktere je unikatni
        if (!res.IsValid)
        {
            _logger.Error("KIndex backup save error\n" + res.ServerError?.ToString());
            throw new ApplicationException(res.ServerError?.ToString());
        }
    }
    
    public static async Task CreateBackupAsync(string comment, string ico, bool useTempDb = false)
    {
        KIndexData kidx = await GetDirectAsync(ico, useTempDb);
        if (kidx == null)
            return;
        await CreateBackupAsync(comment, kidx, useTempDb);
    }
    
    
    //KIndexFeedback
    public static async Task SaveAsync(this KindexFeedback kindexFeedback)
        {
            if (string.IsNullOrWhiteSpace(kindexFeedback.Id))
            {
                kindexFeedback.Id = Guid.NewGuid().ToString();
            }
            if (string.IsNullOrWhiteSpace(kindexFeedback.Company))
            {
                var firma = FirmaRepo.FromIco(kindexFeedback.Ico);
                kindexFeedback.Company = firma.Jmeno;
            }

            var client = Manager.GetESClient_KindexFeedback();
            var res = await client.IndexDocumentAsync(kindexFeedback); //druhy parametr musi byt pole, ktere je unikatni
            if (!res.IsValid)
            {
                throw new ApplicationException(res.ServerError?.ToString());
            }
        }

        public static async Task<IEnumerable<KindexFeedback>> GetKindexFeedbacksAsync(string ico, int year)
        {
            ElasticClient esClient = Manager.GetESClient_KindexFeedback();

            ISearchResponse<KindexFeedback> searchResults = null;
            try
            {
                searchResults = await esClient.SearchAsync<KindexFeedback>(s =>
                        s.Query(q =>
                            q.Term(f => f.Ico, ico)
                            && q.Term(f => f.Year, year)
                            )
                        );

                if (searchResults.IsValid && searchResults.Hits.Count > 0)
                {
                    var hits = searchResults.Hits.Select(h => h.Source).OrderByDescending(s => s.SignDate);
                    return hits;
                }
            }
            catch (Exception e)
            {
                string origQuery = $"ico:{ico}; year:{year};";
                AuditRepo.Add(Audit.Operations.Search, "", "", "KindexFeedback", "error", origQuery, null);
                if (searchResults != null && searchResults.ServerError != null)
                {
                    Manager.LogQueryError<KindexFeedback>(searchResults,
                        $"Exception for {origQuery}",
                        ex: e);
                }
                else
                {
                    _logger.Error(e, origQuery);
                }
            }

            return Enumerable.Empty<KindexFeedback>();
        }

        //prejmenovat na getfeedbackbyidasync
        public static async Task<KindexFeedback> GetByIdAsync(string id)
        {

            ElasticClient esClient = Manager.GetESClient_KindexFeedback();

            ISearchResponse<KindexFeedback> searchResults = null;
            try
            {
                searchResults = await esClient.SearchAsync<KindexFeedback>(s =>
                        s.Query(q =>
                            q.Term(f => f.Id, id)
                            )
                        );

                if (searchResults.IsValid && searchResults.Hits.Count > 0)
                {
                    var hits = searchResults.Hits.Select(h => h.Source).OrderByDescending(s => s.SignDate);
                    return hits.FirstOrDefault();
                }
            }
            catch (Exception e)
            {
                string origQuery = $"id:{id};";
                AuditRepo.Add(Audit.Operations.Search, "", "", "KindexFeedback", "error", origQuery, null);
                if (searchResults != null && searchResults.ServerError != null)
                {
                    Manager.LogQueryError<KindexFeedback>(searchResults,
                        $"Exception for {origQuery}",
                        ex: e);
                }
                else
                {
                    _logger.Error(e, origQuery);
                }
            }

            return null;
        }
}