﻿using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {
        static Devmasters.Cache.AWS_S3.ManagerAsync<StatisticsSubjectPerYear<Firma.Statistics.Subsidy>, Firma>
            _dotaceCache = Devmasters.Cache.AWS_S3.ManagerAsync<StatisticsSubjectPerYear<Firma.Statistics.Subsidy>, Firma>
                .GetSafeInstance("Firma_DotaceStatistics_v2",
                    async (firma) => await CalculateDotaceStatAsync(firma),
                    TimeSpan.Zero,
                    new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
                    keyValueSelector: f => f.ICO);


        static Devmasters.Cache.AWS_S3.Manager<StatisticsSubjectPerYear<Firma.Statistics.Subsidy>, (Firma firma,
                HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost)>
            _holdingDotaceCache = Devmasters.Cache.AWS_S3.Manager<StatisticsSubjectPerYear<Firma.Statistics.Subsidy>, (Firma firma,
                    HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost)>
                .GetSafeInstance("Holding_DotaceStatistics_v3",
                    (obj) => CalculateHoldingDotaceStat(obj.firma, obj.aktualnost),
                    TimeSpan.Zero,
                    new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
                    keyValueSelector: obj => obj.firma.ICO + "-" + obj.aktualnost.ToString());

        public static StatisticsSubjectPerYear<Firma.Statistics.Subsidy> CachedHoldingStatisticsDotace(
            Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost,
            bool forceUpdateCache = false, bool invalidateOnly = false)
        {
            if (forceUpdateCache || invalidateOnly)
                _holdingDotaceCache.Delete((firma, aktualnost));

            if (invalidateOnly)
                return new();
            else
                return _holdingDotaceCache.Get((firma, aktualnost));
        }

        public static StatisticsSubjectPerYear<Firma.Statistics.Subsidy> CachedStatisticsDotace(
            Firma firma,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
            {
                _ = Task.Run(async () => { await _dotaceCache.DeleteAsync(firma); }).Wait(TimeSpan.FromSeconds(10));
            }
            StatisticsSubjectPerYear<Firma.Statistics.Subsidy> ret = new StatisticsSubjectPerYear<Firma.Statistics.Subsidy>();
            _ = Task.Run(async () => { ret = await _dotaceCache.GetAsync(firma); }).Wait(TimeSpan.FromSeconds(20));
            return ret;
        }


        public static void RemoveStatisticsDotace(Firma firma)
        {
            _dotaceCache.DeleteAsync(firma).ConfigureAwait(false).GetAwaiter().GetResult();
            _holdingDotaceCache.Delete((firma, DS.Graphs.Relation.AktualnostType.Aktualni));
            _holdingDotaceCache.Delete((firma, DS.Graphs.Relation.AktualnostType.Nedavny));
            _holdingDotaceCache.Delete((firma, DS.Graphs.Relation.AktualnostType.Libovolny));

        }

        public async static Task<StatisticsSubjectPerYear<Firma.Statistics.Subsidy>> CachedStatisticsDotaceAsync(
        Firma firma,
        bool forceUpdateCache = false)
        {
            await Task.Delay(100);
            if (forceUpdateCache)
            {
                await _dotaceCache.DeleteAsync(firma);
            }
            StatisticsSubjectPerYear<Firma.Statistics.Subsidy> ret = new StatisticsSubjectPerYear<Firma.Statistics.Subsidy>();
            ret = await _dotaceCache.GetAsync(firma);
            return ret;
        }
        private static StatisticsSubjectPerYear<Firma.Statistics.Subsidy> CalculateHoldingDotaceStat(Firma firma,
            HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost)
        {

            var statistiky = firma.Holding(aktualnost)
                .Append(firma)
                .Where(f => f.Valid)
                .Select(f => new { f.ICO, dotaceStats = f.StatistikaDotaci() })
                .ToArray();

            var statistikyPerIco = new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Subsidy>>();

            foreach (var ico in statistiky.Select(m => m.ICO).Distinct())
            {
                statistikyPerIco[ico] = new StatisticsSubjectPerYear<Firma.Statistics.Subsidy>();
                statistikyPerIco[ico] = (StatisticsSubjectPerYear<Firma.Statistics.Subsidy>.Aggregate(
                        statistiky.Where(w => w.ICO == ico).Select(m => m.dotaceStats).ToArray()
                        )
                    ) ?? new StatisticsSubjectPerYear<Firma.Statistics.Subsidy>();
            }

            StatisticsSubjectPerYear<Firma.Statistics.Subsidy> aggregate = Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.Subsidy>.Aggregate(statistikyPerIco.Values);

            foreach (var year in aggregate)
            {
                year.Value.JednotliveFirmy = statistikyPerIco
                    .Where(s => s.Value.StatisticsForYear(year.Year).CelkemPrideleno != 0)
                    .ToDictionary(s => s.Key, s => s.Value.StatisticsForYear(year.Year).CelkemPrideleno);
            }

            return aggregate;
        }

        private static async Task<StatisticsSubjectPerYear<Firma.Statistics.Subsidy>> CalculateDotaceStatAsync(Firma f)
        {
            await Task.Delay(100);
            var dotaceFirmy = await SubsidyRepo.GetDotaceForIcoAsync(f.ICO).ToListAsync();

            // doplnit počty dotací
            var statistiky = dotaceFirmy.GroupBy(d => d.ApprovedYear)
                .ToDictionary(g => g.Key ?? 0,
                    g => new Firma.Statistics.Subsidy()
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
                    yearstat = new Firma.Statistics.Subsidy();
                    statistiky.Add(dy.Key, yearstat);
                }

                yearstat.CelkemPrideleno = dy.Value;
            }


            return new StatisticsSubjectPerYear<Firma.Statistics.Subsidy>(f.ICO, statistiky);
        }
    }
}