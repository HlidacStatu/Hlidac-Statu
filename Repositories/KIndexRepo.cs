using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Entities.KIndex;
using HlidacStatu.Util;
using Nest;
using Serilog;
using Consts = HlidacStatu.Entities.KIndex.Consts;

namespace HlidacStatu.Repositories;

public static class KIndexRepo
{
    private static readonly ILogger _logger = Log.ForContext(typeof(KIndexRepo));
    private static string Best(KIndexData.Annual data, int year, string ico, out KIndexData.KIndexParts? usedPart)
    {
        usedPart = data.OrderedValuesFromBestForInfofacts(ico).FirstOrDefault();
        if (usedPart != null)
        {
            return KIndexData.KIndexCommentForPart(usedPart.Value, data);
        }
        return null;
    }
    private static string Worst(KIndexData.Annual data, int year, string ico, out KIndexData.KIndexParts? usedPart)
    {
        usedPart = data.OrderedValuesFromBestForInfofacts(ico)?.Reverse()?.FirstOrDefault();
        if (usedPart != null)
        {
            return KIndexData.KIndexCommentForPart(usedPart.Value, data);
        }
        return null;
    }


    static Devmasters.Cache.LocalMemory.Manager<KIndexData.KIndexParts[], (KIndexData.Annual, string)> _orderedValuesFromBestForInfofactsCache
        = Devmasters.Cache.LocalMemory.Manager<KIndexData.KIndexParts[], (KIndexData.Annual, string)>.GetSafeInstance("orderedValuesFromBestForInfofacts",
            (data) => _orderedValuesFromBestForInfofacts(data.Item1, data.Item2),
            TimeSpan.FromDays(2), (data) => $"{data.Item1.Ico}_{data.Item1.Rok}-{data.Item2}"
            );

    public static KIndexData.KIndexParts[] OrderedValuesFromBestForInfofacts(this KIndexData.Annual annual, string ico, bool invalidateCache = false)
    {
        if (invalidateCache)
            _orderedValuesFromBestForInfofactsCache.Delete((annual, ico));

        return _orderedValuesFromBestForInfofactsCache.Get((annual, ico));
    }

    static KIndexData.KIndexParts[] _orderedValuesFromBestForInfofacts(this KIndexData.Annual annual, string ico)
    {
        if (annual._orderedValuesForInfofacts == null)
        {
            lock (annual._lockObj)
            {
                if (annual._orderedValuesForInfofacts == null)
                {
                    var stat = Analysis.KorupcniRiziko.Statistics.GetStatistics(annual.Rok); //todo: může být null, co s tím?
                    if (annual.KIndexVypocet.Radky != null || annual.KIndexVypocet.Radky.Count() > 0)

                        annual._orderedValuesForInfofacts = annual.KIndexVypocet.Radky
                            .Select(m => new { r = m, rank = stat.SubjektRank(ico, m.VelicinaPart) })
                            .Where(m => m.rank.HasValue)
                            .Where(m =>
                                    m.r.VelicinaPart != KIndexData.KIndexParts.PercNovaFirmaDodavatel //nezajimava oblast
                                    && !(m.r.VelicinaPart == KIndexData.KIndexParts.PercSmlouvyPod50kBonus && m.r.Hodnota == 0) //bez bonusu
                            )
                            .OrderBy(m => m.rank)
                            .ThenBy(o => o.r.Hodnota)
                            .Select(m => m.r.VelicinaPart)
                            .ToArray(); //better debug
                    else
                        annual._orderedValuesForInfofacts = new KIndexData.KIndexParts[] { };
                }
            }
        }
        return annual._orderedValuesForInfofacts;

    }
    
    public static InfoFact[] InfoFacts(this KIndexData kIndexData, int year)
        {
            var ann = kIndexData.roky.Where(m => m.Rok == year).FirstOrDefault();

            List<InfoFact> facts = new List<InfoFact>();
            if (ann == null || ann.KIndexReady == false)
            {
                facts.Add(new InfoFact($"K-Index nespočítán. Organizace má méně než {Consts.MinPocetSmluvPerYear} smluv za rok nebo malý objem smluv.", InfoFact.ImportanceLevel.Summary));
                return facts.ToArray();
            }
            switch (ann.KIndexLabel)
            {
                case KIndexData.KIndexLabelValues.None:
                    facts.Add(new InfoFact($"K-Index nespočítán. Organizace má méně než {Consts.MinPocetSmluvPerYear} smluv za rok nebo malý objem smluv.", InfoFact.ImportanceLevel.Summary));
                    return facts.ToArray();

                case KIndexData.KIndexLabelValues.A:
                    facts.Add(new InfoFact("Nevykazuje téměř žádné rizikové faktory.", InfoFact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.B:
                    facts.Add(new InfoFact("Chování s malou mírou rizikových faktorů.", InfoFact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.C:
                    facts.Add(new InfoFact("Částečně umožňuje rizikové jednání.", InfoFact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.D:
                    facts.Add(new InfoFact("Vyšší míra rizikových faktorů.", InfoFact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.E:
                    facts.Add(new InfoFact("Vysoký výskyt rizikových faktorů.", InfoFact.ImportanceLevel.Summary));
                    break;
                case KIndexData.KIndexLabelValues.F:
                    facts.Add(new InfoFact("Velmi vysoká míra rizikových faktorů.", InfoFact.ImportanceLevel.Summary));
                    break;
                default:
                    break;
            }
            KIndexData.KIndexParts? bestPart = null;
            KIndexData.KIndexParts? worstPart = null;
            var sBest = Best(ann, year, kIndexData.Ico, out bestPart);
            var sworst = Worst(ann, year, kIndexData.Ico, out worstPart);

            //A-C dej pozitivni prvni
            if (ann.KIndexLabel == KIndexData.KIndexLabelValues.A
                || ann.KIndexLabel == KIndexData.KIndexLabelValues.B
                || ann.KIndexLabel == KIndexData.KIndexLabelValues.C
                )
            {
                if (!string.IsNullOrEmpty(sBest))
                    facts.Add(new InfoFact(sBest, InfoFact.ImportanceLevel.Stat));
                if (!string.IsNullOrEmpty(sworst))
                    facts.Add(new InfoFact(sworst, InfoFact.ImportanceLevel.Stat));
            }
            else
            {
                if (!string.IsNullOrEmpty(sworst))
                    facts.Add(new InfoFact(sworst, InfoFact.ImportanceLevel.Stat));
                if (!string.IsNullOrEmpty(sBest))
                    facts.Add(new InfoFact(sBest, InfoFact.ImportanceLevel.Stat));
            }
            foreach (var part in ann.OrderedValuesFromBestForInfofacts(kIndexData.Ico).Reverse())
            {
                if (part != bestPart && part != worstPart)
                {
                    var sFacts = KIndexData.KIndexCommentForPart(part, ann);
                    if (!string.IsNullOrEmpty(sFacts))
                        facts.Add(new InfoFact(sFacts, InfoFact.ImportanceLevel.High));
                }
            }

            return facts.ToArray();
        }

    public static int[] GetAvailableCalculationYears()
    {
        if (Consts.XAvailableCalculationYears is null || !Consts.XAvailableCalculationYears.Any())
        {
            Consts.XAvailableCalculationYears = Consts.ToCalculationYears
                .Where(r => r <= Analysis.KorupcniRiziko.Statistics.KIndexStatTotal.Get().Max(m => m.Rok))
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
    public static int FixKindexYear(int? year)
    {
        if (year < GetAvailableCalculationYears().Min())
            return GetAvailableCalculationYears().Min();
        if (year is null || year >= GetAvailableCalculationYears().Max())
            return GetAvailableCalculationYears().Max();

        return year.Value;
    }
    
    //KindexData
    //todo: infofacts komplet refaktor? 
    public static InfoFact[] InfoFacts(this KIndexData kIndexData)
    {
        var kidx = kIndexData.LastReadyKIndex();
        return kIndexData.InfoFacts(kidx?.Rok ?? GetAvailableCalculationYears().Max());
    }
    
    public static KIndexData Empty(string ico, string jmeno = null)
    {
        KIndexData kidx = new KIndexData();
        kidx.Ico = ico;
        kidx.Jmeno = jmeno ?? Firmy.GetJmeno(ico);
        kidx.roky = KIndexRepo.GetAvailableCalculationYears()
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
    
    public static async Task<KIndexData> GetDirectAsync((string ico, bool useTempDb) param)
    {
        if (Consts.KIndexExceptions.Contains(param.ico) && param.useTempDb == false)
            return null;

        var client = Manager.GetESClient_KIndex();
        if (param.useTempDb)
            client = Manager.GetESClient_KIndexTemp();


        var res = await client.GetAsync<KIndexData>(param.ico);
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
                    r.Ico = param.ico;
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
        KIndexData kidx = await GetDirectAsync((ico, useTempDb));
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