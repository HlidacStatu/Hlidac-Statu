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
        Firma firma,
        int? obor) =>
        PermanentCache.GetOrSetAsync($"_Firma_SmlouvyStatistics_:{firma.ICO}-{obor?.ToString() ?? "null"}",
            _ => CalculateFirmaSmlouvyStatisticsAsync(firma, obor));

    public static ValueTask InvalidateFirmaSmlouvyStatisticsAsync(Firma firma, int? obor) =>
        PermanentCache.ExpireAsync($"_Firma_SmlouvyStatistics_:{firma.ICO}-{obor?.ToString() ?? "null"}");


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
}