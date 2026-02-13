using Devmasters.DT;
using HlidacStatu.Caching;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories.Statistics;
using Nest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZiggyCreatures.Caching.Fusion;


namespace HlidacStatu.Repositories.Cache;

public class StatisticsCache
{
    private static IFusionCache PermanentCache =>
        HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore, nameof(StatisticsCache));
    
    //smlouvy
    public static ValueTask<StatisticsPerYear<Smlouva.Statistics.Data>>
        GetSmlouvyStatisticsForQueryAsync(string query) =>
        PermanentCache.GetOrSetAsync($"_SmlouvyStatisticsForQuery:{query}",
            _ => SmlouvyStatistics.CalculateAsync(query));

    //Holding Smlouvy
    public static ValueTask<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> GetHoldingSmlouvyStatisticsAsync(
        Firma firma,
        int? obor) =>
        PermanentCache.GetOrSetAsync($"_Holding_SmlouvyStatistics_:{firma.ICO}-{obor?.ToString() ?? "null"}",
            _ => CalculateHoldingSmlouvyStatisticsAsync(firma, obor));

    public static ValueTask InvalidateHoldingSmlouvyStatisticsAsync(Firma firma, int? obor) =>
        PermanentCache.RemoveAsync($"_Holding_SmlouvyStatistics_:{firma.ICO}-{obor?.ToString() ?? "null"}");

    //Firma Smlouvy
    public static ValueTask<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> GetFirmaSmlouvyStatisticsAsync(
        Firma firma, int? obor) =>
        PermanentCache.GetOrSetAsync($"_Firma_SmlouvyStatistics_:{firma.ICO}-{obor?.ToString() ?? "null"}",
            _ => CalculateFirmaSmlouvyStatisticsAsync(firma, obor));

    public static ValueTask InvalidateFirmaSmlouvyStatisticsAsync(Firma firma, int? obor) =>
        PermanentCache.RemoveAsync($"_Firma_SmlouvyStatistics_:{firma.ICO}-{obor?.ToString() ?? "null"}");


    //Osoby cache
    public static ValueTask<Osoba.Statistics.RegistrSmluv> GetOsobaSmlouvyStatisticsAsync(Osoba osoba, int? obor) =>
        PermanentCache.GetOrSetAsync($"_Osoba_SmlouvyStatistics_:{osoba.NameId}-{obor?.ToString() ?? "null"}",
            _ => CalculateSmlouvyStatAsync(osoba, obor));

    public static ValueTask InvalidateOsobaSmlouvyStatisticsAsync(Osoba osoba, int? obor) =>
        PermanentCache.RemoveAsync($"_Osoba_SmlouvyStatistics_:{osoba.NameId}-{obor?.ToString() ?? "null"}");

    //Vz cache
    public static ValueTask<StatisticsSubjectPerYear<Firma.Statistics.VZ>> GetFirmaVzStatisticsAsync(Firma firma) =>
        PermanentCache.GetOrSetAsync($"_Firma_VZStatistics_:{firma.ICO}",
            _ => CalculateVZStatAsync(firma));

    public static ValueTask InvalidateFirmaVzStatisticsAsync(Firma firma) =>
        PermanentCache.RemoveAsync($"_Firma_VZStatistics_:{firma.ICO}");

    public static ValueTask<StatisticsSubjectPerYear<Firma.Statistics.VZ>> GetHoldingVzStatisticsAsync(Firma firma) =>
        PermanentCache.GetOrSetAsync($"_Holding_VZStatistics_:{firma.ICO}",
            _ => CalculateHoldingVZStatAsync(firma));

    public static ValueTask InvalidateHoldingVzStatisticsAsync(Firma firma) =>
        PermanentCache.RemoveAsync($"_Holding_VZStatistics_:{firma.ICO}");


    //Dotace cache
    public static ValueTask<StatisticsSubjectPerYear<Firma.Statistics.Dotace>>
        GetFirmaDotaceStatisticsAsync(Firma firma) =>
        PermanentCache.GetOrSetAsync($"_Firma_DotaceStatistics_:{firma.ICO}",
            _ => CalculateFirmaDotaceStatAsync(firma));

    public static ValueTask InvalidateFirmaDotaceStatisticsAsync(Firma firma) =>
        PermanentCache.RemoveAsync($"_Firma_DotaceStatistics_:{firma.ICO}");

    public static ValueTask<StatisticsSubjectPerYear<Firma.Statistics.Dotace>>
        GetHoldingDotaceStatisticsAsync(Firma firma) =>
        PermanentCache.GetOrSetAsync($"_Holding_DotaceStatistics_:{firma.ICO}",
            _ => CalculateHoldingDotaceStatAsync(firma));

    public static ValueTask InvalidateHoldingDotaceStatisticsAsync(Firma firma) =>
        PermanentCache.RemoveAsync($"_Holding_DotaceStatistics_:{firma.ICO}");
    

    public static ValueTask<Osoba.Statistics.Dotace>
        GetOsobaDotaceStatisticsAsync(Osoba osoba) =>
        PermanentCache.GetOrSetAsync($"_Osoba_DotaceStatistics_:{osoba.NameId}",
            _ => CalculateOsobaDotaceStatAsync(osoba));

    public static ValueTask InvalidateOsobaDotaceStatisticsAsync(Osoba osoba) =>
        PermanentCache.RemoveAsync($"_Osoba_DotaceStatistics_:{osoba.NameId}");
    
    // Firmy global
    // TODO: Krom plnění daty se tato cache vůbec nikde nepoužívá
    public static async Task<GlobalStatisticsPerYear<Smlouva.Statistics.Data>> UradySmlouvyGlobalAsync(int? obor = null,
        GlobalStatisticsPerYear<Smlouva.Statistics.Data> newData = null)
    {
        
        string key = $"UradySmlouvyGlobal_{obor?.ToString() ?? "null"}";

        if (newData != null)
        {
            await PermanentCache.SetAsync(key, newData);
            return newData;
        }

        return await PermanentCache.GetOrSetAsync(key,
            _ => Firmy.GlobalStatistics.CalculateGlobalRankPerYear_UradySmlouvyAsync(obor),
            options =>
            {
                options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365));
            });
    }


    //Factories
    public static async Task<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> CalculateHoldingSmlouvyStatisticsAsync(
        Firma firma,
        int? obor)
    {
        var firmyMaxrok = new Dictionary<string, Devmasters.DT.DateInterval>
            { { firma.ICO, new Devmasters.DT.DateInterval(DateTime.MinValue, DateTime.MaxValue) } };

        var skutecneVazby =
            Relation.SkutecnaDobaVazby(await firma.AktualniVazbyAsync(DS.Graphs.Relation.AktualnostType.Libovolny));
        foreach (var v in skutecneVazby)
        {
            if (!string.IsNullOrEmpty(v.To?.UniqId)
                && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
            {
                firmyMaxrok.TryAdd(v.To.Id,
                    new Devmasters.DT.DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
            }
        }

        var maxConcurrentTasks = 10;
        var semaphore = new SemaphoreSlim(maxConcurrentTasks);

        var statistikyTasks = firmyMaxrok
            .Select(async f =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var _f = await FirmaCache.GetAsync(f.Key);
                    if (_f == null)
                        return new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(f.Key);
                    return new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(f.Key,
                        (await _f.StatistikaRegistruSmluvAsync(obor))
                        .Filter(m => m.Key >= f.Value.From?.Year && m.Key <= f.Value.To?.Year)
                    );
                }
                finally
                {
                    semaphore.Release();
                }
            })
            .ToArray();

        var statistiky = await Task.WhenAll(statistikyTasks);

        if (statistiky.Length == 0)
            return new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(firma.ICO);
        if (statistiky.Length == 1)
            return statistiky[0];

        StatisticsSubjectPerYear<Smlouva.Statistics.Data> aggregate =
            Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>.Aggregate(firma.ICO, statistiky);

        return aggregate;
    }

    public static async Task<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> CalculateHoldingSmlouvyStatisticsQuery_Direct_Async(
    Firma firma,
    Devmasters.DT.DateInterval onlyForInterval = null)
    {
        var firmyMaxrok = new Dictionary<string, Devmasters.DT.DateInterval>
            { { firma.ICO, new Devmasters.DT.DateInterval(DateTime.MinValue, DateTime.MaxValue) } };

        var skutecneVazby =
            Relation.SkutecnaDobaVazby(await firma.AktualniVazbyAsync(DS.Graphs.Relation.AktualnostType.Libovolny));
        foreach (var v in skutecneVazby)
        {
            if (!string.IsNullOrEmpty(v.To?.UniqId)
                && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
            {
                firmyMaxrok.TryAdd(v.To.Id,
                    new Devmasters.DT.DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
            }
        }

        var maxConcurrentTasks = 10;
        var semaphore = new SemaphoreSlim(maxConcurrentTasks);

        var statistikyTasks = firmyMaxrok
            .Select(async f =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var ff = await FirmaCache.GetAsync(f.Key);

                    var intvl = Devmasters.DT.DateInterval.GetOverlappingInterval(onlyForInterval, f.Value);

                    if (ff!= null && intvl != null)
                    {
                        string iOd = intvl?.From?.ToString("yyyy-MM-dd") ?? "*";
                        string iDo = intvl?.To?.ToString("yyyy-MM-dd") ?? "*";

                        var query = $"ico:{ff.ICO} AND podepsano:[{iOd} TO {iDo}] ";

                        var ffStat = await SmlouvyStatistics.CalculateAsync(query);
                        return new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(f.Key, ffStat);
                    }
                    else
                    {
                        return null;
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            })
            .Where(s => s != null)
            .ToArray();

        var statistiky = await Task.WhenAll(statistikyTasks);

        if (statistiky.Length == 0)
            return new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(firma.ICO);
        if (statistiky.Length == 1)
            return statistiky[0];

        StatisticsSubjectPerYear<Smlouva.Statistics.Data> aggregate =
            Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>.Aggregate(firma.ICO, statistiky);

        return aggregate;
    }

    private static async Task<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> CalculateFirmaSmlouvyStatisticsAsync(
        Firma f, int? obor)
    {
        StatisticsSubjectPerYear<Smlouva.Statistics.Data> res = null;
        if (obor.HasValue && obor != 0)
            res = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                f.ICO,
                await SmlouvyStatistics.CalculateAsync(
                    $"ico:{f.ICO} AND oblast:{Smlouva.SClassification.Classification.ClassifSearchQuery(obor.Value)}")
            );
        else
            res = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                f.ICO,
                await SmlouvyStatistics.CalculateAsync($"ico:{f.ICO}")
            );

        res ??= new StatisticsSubjectPerYear<Smlouva.Statistics.Data>();
        return res;
    }

    private static async Task<Osoba.Statistics.RegistrSmluv> CalculateSmlouvyStatAsync(Osoba o, int? obor)
    {
        Osoba.Statistics.RegistrSmluv res = new Osoba.Statistics.RegistrSmluv();
        res.OsobaNameId = o.NameId;
        res.Obor = (Smlouva.SClassification.ClassificationsTypes?)obor;

        Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>> statni =
            new Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>>();
        Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>> soukr =
            new Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>>();
        Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>> nezisk =
            new Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>>();

        var skutecneVazby = Relation.SkutecnaDobaVazby(
            await o.AktualniVazbyAsync( 
                Relation.CharakterVazbyEnum.VlastnictviKontrola, 
                Relation.AktualnostType.Libovolny)
            );
        var firmy_maxrok = new Dictionary<string, Devmasters.DT.DateInterval>();
        foreach (var v in skutecneVazby.Where(v => !string.IsNullOrEmpty(v.To?.UniqId)
                                                   && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType
                                                       .Company))
        {
            firmy_maxrok.TryAdd(v.To.Id,
                new Devmasters.DT.DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
        }

        var perIcoStatList = new ConcurrentBag<(Firma firma, StatisticsSubjectPerYear<Smlouva.Statistics.Data> ss)>();
        
        await Parallel.ForEachAsync(firmy_maxrok, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (kvp, ct) =>
        {
            var firma = await FirmaCache.GetAsync(kvp.Key);
            if (firma?.Valid == true)
            {
                var stats = await firma.StatistikaRegistruSmluvAsync();
                var result = (firma, 
                    new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                    firma.ICO,
                    stats.Filter(fi => fi.Key >= kvp.Value.From?.Year && fi.Key <= kvp.Value.To?.Year)));
                
                
                perIcoStatList.Add(result);
            }
        });

        foreach (var it in perIcoStatList)
        {
            if (it.firma.PatrimStatu() && statni.ContainsKey(it.firma.ICO) == false)
                statni.Add(it.firma.ICO, it.ss);
            else if (it.firma.JsemNeziskovka() && nezisk.ContainsKey(it.firma.ICO) == false)
                nezisk.Add(it.firma.ICO, it.ss);
            else if (soukr.ContainsKey(it.firma.ICO) == false)
                soukr.Add(it.firma.ICO, it.ss);
        }

        res.StatniFirmy = statni;
        res.SoukromeFirmy = soukr;
        res.Neziskovky = nezisk;
        return res;
    }

    private static async Task<StatisticsSubjectPerYear<Firma.Statistics.VZ>> CalculateHoldingVZStatAsync(Firma firma)
    {
        var firmy_maxrok = new Dictionary<string, Devmasters.DT.DateInterval>();
        firmy_maxrok.Add(firma.ICO, new Devmasters.DT.DateInterval(DateTime.MinValue, DateTime.MaxValue));
        var skutecneVazby =
            Relation.SkutecnaDobaVazby(await firma.AktualniVazbyAsync(DS.Graphs.Relation.AktualnostType.Libovolny));
        foreach (var v in skutecneVazby)
        {
            if (!string.IsNullOrEmpty(v.To?.UniqId)
                && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
            {
                firmy_maxrok.TryAdd(v.To.Id,
                    new Devmasters.DT.DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
            }
        }

        var semaphore = new SemaphoreSlim(10);
        var statistiky = await Task.WhenAll(
            firmy_maxrok.Select(async f =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var stat = await (await FirmaCache.GetAsync(f.Key)).StatistikaVerejneZakazkyAsync();
                    return new StatisticsSubjectPerYear<Firma.Statistics.VZ>(
                        f.Key,
                        stat.Filter(m => m.Key >= f.Value.From?.Year && m.Key <= f.Value.To?.Year)
                    );
                }
                finally
                {
                    semaphore.Release();
                }
            })
        );

        if (statistiky.Length == 0)
            return new StatisticsSubjectPerYear<Firma.Statistics.VZ>(firma.ICO);
        if (statistiky.Length == 1)
            return statistiky[0];

        StatisticsSubjectPerYear<Firma.Statistics.VZ> aggregate =
            Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.VZ>.Aggregate(firma.ICO, statistiky);

        return aggregate;
    }

    private static async Task<StatisticsSubjectPerYear<Firma.Statistics.VZ>> CalculateVZStatAsync(Firma f)
    {
        var resVZdodavTask = VZPerYearAsync("icododavatel:" + f.ICO, HlidacStatu.Lib.Analytics.Consts.VZYearsList);
        var resVZzadavTask = VZPerYearAsync("icozadavatel:" + f.ICO, HlidacStatu.Lib.Analytics.Consts.VZYearsList);
        StatisticsPerYear<SimpleStat> resVZdodav = await resVZdodavTask;
        StatisticsPerYear<SimpleStat> resVZzadav = await resVZzadavTask;

        Dictionary<int, Firma.Statistics.VZ> data = new Dictionary<int, Firma.Statistics.VZ>();
        foreach (var year in HlidacStatu.Lib.Analytics.Consts.VZYearsList)
        {
            var stat = new Firma.Statistics.VZ();

            if (resVZdodav.Any(m => m.Year == year))
            {
                stat.PocetJakoDodavatel = resVZdodav.FirstOrDefault(m => m.Year == year).Value.Pocet;
                stat.CelkovaHodnotaJakoDodavatel = resVZdodav.FirstOrDefault(m => m.Year == year).Value.CelkemCena;
            }

            if (resVZzadav.Any(m => m.Year == year))
            {
                stat.PocetJakoZadavatel = resVZzadav.FirstOrDefault(m => m.Year == year).Value.Pocet;
                stat.CelkovaHodnotaJakoZadavatel = resVZzadav.FirstOrDefault(m => m.Year == year).Value.CelkemCena;
            }

            data.Add(year, stat);
        }

        return new StatisticsSubjectPerYear<Firma.Statistics.VZ>(f.ICO, data);
    }

    public static async Task<StatisticsPerYear<SimpleStat>> VZPerYearAsync(string query, int[] interestedInYearsOnly)
    {
        AggregationContainerDescriptor<VerejnaZakazka> aggYSum =
            new AggregationContainerDescriptor<VerejnaZakazka>()
                .DateHistogram("x-agg", h => h
                    .Field(f => f.PosledniZmena)
                    .CalendarInterval(Nest.DateInterval.Year)
                    .Aggregations(agg => agg
                        .Sum("sumincome", s => s
                            .Field(ff => ff.KonecnaHodnotaBezDPH)
                        )
                    )
                );

        var res = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(query, new string[] { }, 1, 0,
            "0", exactNumOfResults: true, anyAggregation: aggYSum);


        Dictionary<int, SimpleStat> result = new Dictionary<int, SimpleStat>();
        if (interestedInYearsOnly != null)
        {
            foreach (int year in interestedInYearsOnly)
            {
                result.Add(year, new SimpleStat());
            }

            foreach (DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["x-agg"]).Items)
            {
                if (result.ContainsKey(val.Date.Year))
                {
                    result[val.Date.Year].Pocet = val.DocCount ?? 0;
                    result[val.Date.Year].CelkemCena =
                        (decimal)(((DateHistogramBucket)val).Sum("sumincome").Value ?? 0);
                }
            }
        }
        else
        {
            foreach (DateHistogramBucket val in ((BucketAggregate)res.ElasticResults.Aggregations["x-agg"]).Items)
            {
                result.Add(val.Date.Year, new SimpleStat());
                result[val.Date.Year].Pocet = val.DocCount ?? 0;
                result[val.Date.Year].CelkemCena =
                    (decimal)(((DateHistogramBucket)val).Sum("sumincome").Value ?? 0);
            }
        }

        return new StatisticsPerYear<SimpleStat>(result);
    }

    private static async Task<StatisticsSubjectPerYear<Firma.Statistics.Dotace>>
        CalculateHoldingDotaceStatAsync(Firma firma)
    {
        var firmy_maxrok = new Dictionary<string, Devmasters.DT.DateInterval>();
        firmy_maxrok.Add(firma.ICO, new Devmasters.DT.DateInterval(DateTime.MinValue, DateTime.MaxValue));
        var skutecneVazby =
            Relation.SkutecnaDobaVazby(await firma.AktualniVazbyAsync(DS.Graphs.Relation.AktualnostType.Libovolny));
        foreach (var v in skutecneVazby)
        {
            if (!string.IsNullOrEmpty(v.To?.UniqId)
                && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
            {
                firmy_maxrok.TryAdd(v.To.Id,
                    new Devmasters.DT.DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
            }
        }

        var semaphore = new SemaphoreSlim(10);
        var statistiky = await Task.WhenAll(
            firmy_maxrok.Select(async f =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var ff = await FirmaCache.GetAsync(f.Key);

                    StatisticsSubjectPerYear<Firma.Statistics.Dotace> stat = new();
                    if (ff != null)
                        stat = await ff.StatistikaDotaciAsync();

                    return new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(
                        f.Key,
                        stat.Filter(m => m.Key >= f.Value.From?.Year && m.Key <= f.Value.To?.Year)
                    );
                }
                finally
                {
                    semaphore.Release();
                }
            })
        );

        if (statistiky.Length == 0)
            return new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(firma.ICO);
        if (statistiky.Length == 1)
            return statistiky[0];

        Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> statistikyPerIco =
            new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();

        foreach (var ico in statistiky.Select(m => m.ICO).Where(m => !string.IsNullOrEmpty(m)))
        {
            statistikyPerIco[ico] = new StatisticsSubjectPerYear<Firma.Statistics.Dotace>();
            statistikyPerIco[ico] = (StatisticsSubjectPerYear<Firma.Statistics.Dotace>.Aggregate(firma.ICO,
                        statistiky
                            .Where(w => w.ICO == ico)
                            .Select(m => m)
                            .ToArray()
                    )
                ) ?? new StatisticsSubjectPerYear<Firma.Statistics.Dotace>();
        }

        StatisticsSubjectPerYear<Firma.Statistics.Dotace> aggregate =
            Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.Dotace>.Aggregate(firma.ICO,
                statistikyPerIco.Values);

        foreach (var year in aggregate)
        {
            year.Value.JednotliveFirmy = statistikyPerIco
                .Where(s => s.Value.StatisticsForYear(year.Year).CelkemPrideleno != 0)
                .ToDictionary(s => s.Key, s => s.Value.StatisticsForYear(year.Year).CelkemPrideleno);
        }

        return aggregate;
    }

    private static async Task<StatisticsSubjectPerYear<Firma.Statistics.Dotace>> CalculateFirmaDotaceStatAsync(Firma f)
    {
        var dotaceFirmy = await DotaceRepo.GetDotaceForIcoAsync(f.ICO).ToListAsync();

        // doplnit počty dotací
        var statistiky = dotaceFirmy.GroupBy(d => d.ApprovedYear)
            .ToDictionary(g => g.Key ?? 0,
                g => new Firma.Statistics.Dotace()
                {
                    PocetDotaci = g.Count()
                }
            );

        var dataYearly = dotaceFirmy
            .GroupBy(c => c.ApprovedYear)
            .ToDictionary(g => g.Key ?? 0,
                g => g.Sum(c => c.AssumedAmount)
            );

        foreach (var dy in dataYearly)
        {
            if (!statistiky.TryGetValue(dy.Key, out var yearstat))
            {
                yearstat = new Firma.Statistics.Dotace();
                statistiky.Add(dy.Key, yearstat);
            }

            yearstat.CelkemPrideleno = dy.Value;
        }


        return new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(f.ICO, statistiky);
    }


    private static async Task<Osoba.Statistics.Dotace> CalculateOsobaDotaceStatAsync(Osoba o)
    {
        Osoba.Statistics.Dotace res = new Osoba.Statistics.Dotace();
        res.OsobaNameId = o.NameId;

        Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> statni =
            new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();
        Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> soukr =
            new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();
        Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> nezisk =
            new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();

        var skutecneVazby = Relation.SkutecnaDobaVazby(await o.AktualniVazbyAsync( Relation.CharakterVazbyEnum.VlastnictviKontrola, Relation.AktualnostType.Libovolny));
        var firmy_maxrok = new Dictionary<string, Devmasters.DT.DateInterval>();
        foreach (var v in skutecneVazby.Where(v => !string.IsNullOrEmpty(v.To?.UniqId)
                                                   && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType
                                                       .Company)
                )
        {
            firmy_maxrok.TryAdd(v.To.Id,
                new Devmasters.DT.DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
        }
        
        var perIcoStat = new ConcurrentBag<(Firma Firma, StatisticsSubjectPerYear<Firma.Statistics.Dotace> DotaceStat)>();
        
        await Parallel.ForEachAsync(firmy_maxrok, new ParallelOptions { MaxDegreeOfParallelism = 20 }, async (kvp, ct) =>
        {
            var firma = await FirmaCache.GetAsync(kvp.Key);
            if (firma?.Valid == true)
            {
                var stat = await firma.StatistikaDotaciAsync();
                (Firma Firma, StatisticsSubjectPerYear<Firma.Statistics.Dotace>) result = (firma: firma, 
                    new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(
                        firma.ICO,
                        stat.Filter(fi => fi.Key >= kvp.Value.From?.Year && fi.Key <= kvp.Value.To?.Year)));
        
                perIcoStat.Add(result);
            }
        });

        foreach (var fStat in perIcoStat)
        {
            if (fStat.Firma.PatrimStatu() && statni.ContainsKey(fStat.Firma.ICO) == false)
                statni.Add(fStat.Firma.ICO, fStat.DotaceStat);
            else if (fStat.Firma.JsemNeziskovka() && nezisk.ContainsKey(fStat.Firma.ICO) == false)
                nezisk.Add(fStat.Firma.ICO, fStat.DotaceStat);
            else if (soukr.ContainsKey(fStat.Firma.ICO) == false)
                soukr.Add(fStat.Firma.ICO, fStat.DotaceStat);
        }

        res.StatniFirmy = statni;
        res.SoukromeFirmy = soukr;
        res.Neziskovky = nezisk;

        return res;
    }
}