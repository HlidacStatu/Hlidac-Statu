using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;

using System;
using System.Linq;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {
        static Devmasters.Cache.Couchbase.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, Firma>
           _VZCache = Devmasters.Cache.Couchbase.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, Firma>
               .GetSafeInstance("Firma_VZStatistics",
                   (firma) => CalculateVZStat(firma),
                   TimeSpan.FromHours(12),
                   Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                   Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                   Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                   Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                   f => f.ICO);


        static Devmasters.Cache.Couchbase.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, (Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost)>
           _holdingVZCache = Devmasters.Cache.Couchbase.Manager<StatisticsSubjectPerYear<Firma.Statistics.VZ>, (Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost)>
               .GetSafeInstance("Holding_VZStatistics",
                   (obj) => CalculateHoldingVZStat(obj.firma, obj.aktualnost),
                   TimeSpan.FromHours(12),
                   Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                   Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                   Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                   Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                   obj => obj.firma.ICO + "-" + obj.aktualnost.ToString());

        public static StatisticsSubjectPerYear<Firma.Statistics.VZ> CachedHoldingStatisticsVZ(
    Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost,
    bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _holdingVZCache.Delete((firma, aktualnost));

            return _holdingVZCache.Get((firma, aktualnost));
        }

        public static StatisticsSubjectPerYear<Firma.Statistics.VZ> CachedStatisticsVZ(
            Firma firma,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _VZCache.Delete(firma);

            return _VZCache.Get(firma);
        }

        private static StatisticsSubjectPerYear<Firma.Statistics.VZ> CalculateHoldingVZStat(Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost)
        {
            var firmy = firma.Holding(aktualnost).ToArray();

            var statistiky = firmy.Select(f => f.StatistikaVerejneZakazky()).Append(firma.StatistikaVerejneZakazky()).ToArray();

            var aggregate = Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.VZ>.Aggregate(statistiky);

            return aggregate;

        }

        private static StatisticsSubjectPerYear<Firma.Statistics.VZ> CalculateVZStat(Firma f)
        {
            throw new NotImplementedException();
            //var VZFirmy = DotaceRepo.GetVZForIco(f.ICO);

            //// doplnit počty dotací
            //var statistiky = VZFirmy.GroupBy(d => d.DatumPodpisu?.Year)
            //    .ToDictionary(g => g.Key ?? 0,
            //        g => new Firma.Statistics.VZ()
            //        {
            //            PocetDotaci = g.Count()
            //        }
            //    );

            //var cerpani = VZFirmy
            //    .SelectMany(d => d.Rozhodnuti)
            //    .SelectMany(r => r.Cerpani);

            //var dataYearly = cerpani
            //    .GroupBy(c => c.GuessedYear)
            //    .ToDictionary(g => g.Key ?? 0,
            //        g => (CelkemCerpano: g.Sum(c => c.CastkaSpotrebovana ?? 0),
            //            PocetCerpani: g.Count(c => c.CastkaSpotrebovana.HasValue))
            //    );

            //foreach (var dy in dataYearly)
            //{
            //    if (!statistiky.TryGetValue(dy.Key, out var yearstat))
            //    {
            //        yearstat = new Firma.Statistics.VZ();
            //        statistiky.Add(dy.Key, yearstat);
            //    }

            //    yearstat.CelkemCerpano = dy.Value.CelkemCerpano;
            //    yearstat.PocetCerpani = dy.Value.PocetCerpani;
            //}


            //return new StatisticsSubjectPerYear<Firma.Statistics.VZ>(f.ICO, statistiky);
        }

    }
}
