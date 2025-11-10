using Devmasters.DT;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {
        static Devmasters.Cache.Memcached.Manager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, Firma>
            _dotaceCache = Devmasters.Cache.Memcached.Manager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, Firma>
                .GetSafeInstance("Firma_DotaceStatistics_v2",
                     (firma) => CalculateDotaceStatAsync(firma).ConfigureAwait(false).GetAwaiter().GetResult(),
                    TimeSpan.Zero,
                    Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(','),
                    keyValueSelector: f => f.ICO);


        static Devmasters.Cache.Memcached.Manager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, Firma>
            _holdingDotaceCache = Devmasters.Cache.Memcached.Manager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, Firma>
                .GetSafeInstance("Holding_DotaceStatistics_v3",
                    (f) => _calculateHoldingDotaceStat(f),
                    TimeSpan.Zero,
                    Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(','),
                    keyValueSelector: f => f.ICO);

        public static StatisticsSubjectPerYear<Firma.Statistics.Dotace> CachedHoldingStatisticsDotace(
            Firma firma, bool forceUpdateCache = false, bool invalidateOnly = false)
        {
            if (forceUpdateCache || invalidateOnly)
                _holdingDotaceCache.Delete(firma);

            if (invalidateOnly)
                return new();
            else
                return _holdingDotaceCache.Get(firma);
        }

        public static StatisticsSubjectPerYear<Firma.Statistics.Dotace> CachedStatisticsDotace(
            Firma firma,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
            {
                _dotaceCache.Delete(firma);
            }
            StatisticsSubjectPerYear<Firma.Statistics.Dotace> ret = new StatisticsSubjectPerYear<Firma.Statistics.Dotace>();
            ret = _dotaceCache.Get(firma);
            return ret;
        }


        public static void RemoveStatisticsDotace(Firma firma)
        {
            _dotaceCache.Delete(firma);
            _holdingDotaceCache.Delete(firma);

        }

        public async static Task<StatisticsSubjectPerYear<Firma.Statistics.Dotace>> CachedStatisticsDotaceAsync(
        Firma firma,
        bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
            {
                _dotaceCache.Delete(firma);
            }
            StatisticsSubjectPerYear<Firma.Statistics.Dotace> ret = new StatisticsSubjectPerYear<Firma.Statistics.Dotace>();
            ret = _dotaceCache.Get(firma);
            return ret;
        }

        static FirmaByIcoComparer byIcoOnly = new FirmaByIcoComparer();
        private static StatisticsSubjectPerYear<Firma.Statistics.Dotace> _calculateHoldingDotaceStat_old(Firma firma,
            HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost)
        {

            var statistiky = firma.Holding(aktualnost)
                .Append(firma)
                .Where(f => f.Valid)
                .Distinct(byIcoOnly)
                .Select(f => new { f.ICO, dotaceStats = f.StatistikaDotaci() })
                .Where(m => !string.IsNullOrEmpty(m.ICO))
                .ToArray();

            if (statistiky.Length == 0)
                return new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(firma.ICO);
            if (statistiky.Length == 1)
                return statistiky[0].dotaceStats;

            Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> statistikyPerIco =
                new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();

            foreach (var ico in statistiky.Select(m => m.ICO).Where(m => !string.IsNullOrEmpty(m)))
            {
                statistikyPerIco[ico] = new StatisticsSubjectPerYear<Firma.Statistics.Dotace>();
                statistikyPerIco[ico] = (StatisticsSubjectPerYear<Firma.Statistics.Dotace>.Aggregate(firma.ICO,
                        statistiky.Where(w => w.ICO == ico).Select(m => m.dotaceStats).ToArray()
                        )
                    ) ?? new StatisticsSubjectPerYear<Firma.Statistics.Dotace>();
            }

            StatisticsSubjectPerYear<Firma.Statistics.Dotace> aggregate =
                Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.Dotace>.Aggregate(firma.ICO, statistikyPerIco.Values);

            foreach (var year in aggregate)
            {
                year.Value.JednotliveFirmy = statistikyPerIco
                    .Where(s => s.Value.StatisticsForYear(year.Year).CelkemPrideleno != 0)
                    .ToDictionary(s => s.Key, s => s.Value.StatisticsForYear(year.Year).CelkemPrideleno);
            }

            return aggregate;
        }

        private static StatisticsSubjectPerYear<Firma.Statistics.Dotace> _calculateHoldingDotaceStat(Firma firma)
        {

            var firmy_maxrok = new Dictionary<string, DateInterval>();
            firmy_maxrok.Add(firma.ICO, new DateInterval(DateTime.MinValue, DateTime.MaxValue));
            var skutecneVazby = Relation.SkutecnaDobaVazby(firma.AktualniVazby(DS.Graphs.Relation.AktualnostType.Libovolny));
            foreach (var v in skutecneVazby)
            {
                if (!string.IsNullOrEmpty(v.To?.UniqId)
                            && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                {
                    firmy_maxrok.TryAdd(v.To.Id, new DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
                }

            }
            var statistiky = firmy_maxrok
                .Select(f => new
                {
                    ICO = f.Key,
                    dotaceStats = new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(
                            f.Key,
                            Firmy.Get(f.Key).StatistikaDotaci().Filter(m => m.Key >= f.Value.From?.Year && m.Key <= f.Value.To?.Year)
                            )
                }
                )
                .ToArray();

            if (statistiky.Length == 0)
                return new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(firma.ICO);
            if (statistiky.Length == 1)
                return statistiky[0].dotaceStats;

            Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> statistikyPerIco =
                new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();

            foreach (var ico in statistiky.Select(m => m.ICO).Where(m => !string.IsNullOrEmpty(m)))
            {
                statistikyPerIco[ico] = new StatisticsSubjectPerYear<Firma.Statistics.Dotace>();
                statistikyPerIco[ico] = (StatisticsSubjectPerYear<Firma.Statistics.Dotace>.Aggregate(firma.ICO,
                        statistiky
                            .Where(w => w.ICO == ico)
                            .Select(m => m.dotaceStats)
                            .ToArray()
                        )
                    ) ?? new StatisticsSubjectPerYear<Firma.Statistics.Dotace>();
            }

            StatisticsSubjectPerYear<Firma.Statistics.Dotace> aggregate =
                Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.Dotace>.Aggregate(firma.ICO, statistikyPerIco.Values);

            foreach (var year in aggregate)
            {
                year.Value.JednotliveFirmy = statistikyPerIco
                    .Where(s => s.Value.StatisticsForYear(year.Year).CelkemPrideleno != 0)
                    .ToDictionary(s => s.Key, s => s.Value.StatisticsForYear(year.Year).CelkemPrideleno);
            }

            return aggregate;
        }



        private static async Task<StatisticsSubjectPerYear<Firma.Statistics.Dotace>> CalculateDotaceStatAsync(Firma f)
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
    }
}