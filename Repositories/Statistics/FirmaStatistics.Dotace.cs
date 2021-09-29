using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities.Analysis;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {
        static Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, Firma>
            _dotaceCache = Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, Firma>
                .GetSafeInstance("Firma_DotaceStatistics",
                    (firma) => CalculateDotaceStat(firma),
                    TimeSpan.FromDays(7),
                    Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                    Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                    Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                    f => f.ICO);


        static Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, (Firma firma,
                Datastructures.Graphs.Relation.AktualnostType aktualnost)>
            _holdingDotaceCache = Util.Cache
                .CouchbaseCacheManager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, (Firma firma,
                    Datastructures.Graphs.Relation.AktualnostType aktualnost)>
                .GetSafeInstance("Holding_DotaceStatistics_v2",
                    (obj) => CalculateHoldingDotaceStat(obj.firma, obj.aktualnost),
                    TimeSpan.FromDays(7),
                    Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                    Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                    Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                    obj => obj.firma.ICO + "-" + obj.aktualnost.ToString());

        public static StatisticsSubjectPerYear<Firma.Statistics.Dotace> CachedHoldingStatisticsDotace(
            Firma firma, Datastructures.Graphs.Relation.AktualnostType aktualnost,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _holdingDotaceCache.Delete((firma, aktualnost));

            return _holdingDotaceCache.Get((firma, aktualnost));
        }

        public static StatisticsSubjectPerYear<Firma.Statistics.Dotace> CachedStatisticsDotace(
            Firma firma,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _dotaceCache.Delete(firma);

            return _dotaceCache.Get(firma);
        }

        private static StatisticsSubjectPerYear<Firma.Statistics.Dotace> CalculateHoldingDotaceStat(Firma firma,
            Datastructures.Graphs.Relation.AktualnostType aktualnost)
        {
            var statistikyPerIco = firma.Holding(aktualnost)
                .Append(firma)
                .Where(f => f.Valid)
                .Select(f => new { f.ICO, dotaceStats = f.StatistikaDotaci() })
                .ToDictionary(s => s.ICO, s => s.dotaceStats);

            var aggregate = Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.Dotace>.Aggregate(statistikyPerIco.Values);

            foreach (var year in aggregate)
            {
                year.Value.JednotliveFirmy = statistikyPerIco
                    .Where(s => s.Value.StatisticsForYear(year.Year).CelkemCerpano != 0)
                    .ToDictionary(s => s.Key, s => s.Value.StatisticsForYear(year.Year).CelkemCerpano);
            }

            return aggregate;
        }

        private static StatisticsSubjectPerYear<Firma.Statistics.Dotace> CalculateDotaceStat(Firma f)
        {
            var dotaceFirmy = DotaceRepo.GetDotaceForIco(f.ICO);

            // doplnit počty dotací
            var statistiky = dotaceFirmy.GroupBy(d => d.DatumPodpisu?.Year)
                .ToDictionary(g => g.Key ?? 0,
                    g => new Firma.Statistics.Dotace()
                    {
                        PocetDotaci = g.Count()
                    }
                );

            var cerpani = dotaceFirmy
                .SelectMany(d => d.Rozhodnuti)
                .SelectMany(r => r.Cerpani);

            var dataYearly = cerpani
                .GroupBy(c => c.GuessedYear)
                .ToDictionary(g => g.Key ?? 0,
                    g => (CelkemCerpano: g.Sum(c => c.CastkaSpotrebovana ?? 0),
                        PocetCerpani: g.Count(c => c.CastkaSpotrebovana.HasValue))
                );

            foreach (var dy in dataYearly)
            {
                if (!statistiky.TryGetValue(dy.Key, out var yearstat))
                {
                    yearstat = new Firma.Statistics.Dotace();
                    statistiky.Add(dy.Key, yearstat);
                }

                yearstat.CelkemCerpano = dy.Value.CelkemCerpano;
                yearstat.PocetCerpani = dy.Value.PocetCerpani;
            }


            return new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(f.ICO, statistiky);
        }
    }
}