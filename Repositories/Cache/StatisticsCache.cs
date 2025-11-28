using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Devmasters.DT;
using Hlidacstatu.Caching;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using HlidacStatu.Repositories.Statistics;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories.Cache;

public class StatisticsCache
{
    private static readonly IFusionCache PermanentCache =
        Hlidacstatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore, nameof(StatisticsCache));


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
        PermanentCache.ExpireAsync($"_Holding_SmlouvyStatistics_:{firma.ICO}-{obor?.ToString() ?? "null"}");

    //Firma Smlouvy
    public static ValueTask<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> GetFirmaSmlouvyStatisticsAsync(
        Firma firma, int? obor) =>
        PermanentCache.GetOrSetAsync($"_Firma_SmlouvyStatistics_:{firma.ICO}-{obor?.ToString() ?? "null"}",
            _ => CalculateFirmaSmlouvyStatisticsAsync(firma, obor));

    public static ValueTask InvalidateFirmaSmlouvyStatisticsAsync(Firma firma, int? obor) =>
        PermanentCache.ExpireAsync($"_Firma_SmlouvyStatistics_:{firma.ICO}-{obor?.ToString() ?? "null"}");


    //Osoby cache
    public static ValueTask<Osoba.Statistics.RegistrSmluv> GetOsobaSmlouvyStatisticsAsync(Osoba osoba, int? obor) =>
        PermanentCache.GetOrSetAsync($"_Osoba_SmlouvyStatistics_:{osoba.NameId}-{obor?.ToString() ?? "null"}",
            _ => CalculateSmlouvyStatAsync(osoba, obor));

    public static ValueTask InvalidateOsobaSmlouvyStatisticsAsync(Osoba osoba, int? obor) =>
        PermanentCache.ExpireAsync($"_Osoba_SmlouvyStatistics_:{osoba.NameId}-{obor?.ToString() ?? "null"}");


    //Factories
    private static async Task<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> CalculateHoldingSmlouvyStatisticsAsync(
        Firma firma,
        int? obor)
    {
        var firmyMaxrok = new Dictionary<string, DateInterval>
            { { firma.ICO, new DateInterval(DateTime.MinValue, DateTime.MaxValue) } };
        var skutecneVazby =
            Relation.SkutecnaDobaVazby(firma.AktualniVazby(DS.Graphs.Relation.AktualnostType.Libovolny));
        foreach (var v in skutecneVazby)
        {
            if (!string.IsNullOrEmpty(v.To?.UniqId)
                && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
            {
                firmyMaxrok.TryAdd(v.To.Id,
                    new DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
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
                    return new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(f.Key,
                        (await Firmy.Get(f.Key).StatistikaRegistruSmluvAsync(obor))
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

        var skutecneVazby = Relation.SkutecnaDobaVazby(o.AktualniVazby(Relation.AktualnostType.Libovolny));
        var firmy_maxrok = new Dictionary<string, DateInterval>();
        foreach (var v in skutecneVazby.Where(v => !string.IsNullOrEmpty(v.To?.UniqId)
                                                   && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType
                                                       .Company))
        {
            firmy_maxrok.TryAdd(v.To.Id,
                new DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
        }

        var maxConcurrentTasks = 10;
        var semaphore = new SemaphoreSlim(maxConcurrentTasks);

        var perIcoStatTasks = firmy_maxrok
            .Select(m => new { firma = Firmy.Get(m.Key), interval = m.Value })
            .Where(m => m.firma.Valid == true)
            .Select(async m =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var stats = await m.firma.StatistikaRegistruSmluvAsync();
                    return new
                    {
                        f = m.firma,
                        ss = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                            m.firma.ICO,
                            stats.Filter(fi => fi.Key >= m.interval.From?.Year && fi.Key <= m.interval.To?.Year)
                        )
                    };
                }
                finally
                {
                    semaphore.Release();
                }
            })
            .ToArray();

        var perIcoStat = await Task.WhenAll(perIcoStatTasks);

        foreach (var it in perIcoStat)
        {
            if (it.f.PatrimStatu() && statni.ContainsKey(it.f.ICO) == false)
                statni.Add(it.f.ICO, it.ss);
            else if (it.f.JsemNeziskovka() && nezisk.ContainsKey(it.f.ICO) == false)
                nezisk.Add(it.f.ICO, it.ss);
            else if (soukr.ContainsKey(it.f.ICO) == false)
                soukr.Add(it.f.ICO, it.ss);
        }

        res.StatniFirmy = statni;
        res.SoukromeFirmy = soukr;
        res.Neziskovky = nezisk;
        return res;
    }
}