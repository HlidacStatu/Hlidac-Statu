using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {

        static Devmasters.Cache.Couchbase.Manager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, Datastructures.Graphs.Relation.AktualnostType aktualnost, int? obor)> 
            _holdingSmlouvaCache
            = Devmasters.Cache.Couchbase.Manager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, Datastructures.Graphs.Relation.AktualnostType aktualnost, int? obor)>
                .GetSafeInstance("Holding_SmlouvyStatistics_v1_",
                    (obj) => _holdingCalculateStats(obj.firma, obj.aktualnost, obj.obor),
                    TimeSpan.FromHours(12),
                    Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                    Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                    Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                    obj => obj.firma.ICO + "-" + obj.aktualnost.ToString() + "-" + (obj.obor ?? 0));

        public static StatisticsSubjectPerYear<Smlouva.Statistics.Data> CachedHoldingStatisticsSmlouvy(
            Firma firma, Datastructures.Graphs.Relation.AktualnostType aktualnost, int? obor = null, 
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _holdingSmlouvaCache.Delete((firma, aktualnost, obor));

            return _holdingSmlouvaCache.Get((firma, aktualnost, obor));
        }

        private static StatisticsSubjectPerYear<Smlouva.Statistics.Data> _holdingCalculateStats(
            Firma f, Datastructures.Graphs.Relation.AktualnostType aktualnost, int? obor)
        {
            var firmy = f.Holding(aktualnost).ToArray();

            var statistiky = firmy.Select(f => f.StatistikaRegistruSmluv(obor)).Append(f.StatistikaRegistruSmluv(obor))
                .ToArray();

            var aggregate = Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>.Aggregate(statistiky);

            return aggregate;

        }

        static Devmasters.Cache.Couchbase.Manager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, int? obor)> _smlouvaCache
            = Devmasters.Cache.Couchbase.Manager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, int? obor)>
                .GetSafeInstance("Firma_SmlouvyStatistics_v3_",
                    (obj) => _calculateSmlouvyStatsAsync(obj.firma, obj.obor).ConfigureAwait(false).GetAwaiter().GetResult(),
                    TimeSpan.FromHours(12),
                    Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                    Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                    Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                    obj => obj.firma.ICO + "-" + (obj.obor ?? 0));

        public static StatisticsSubjectPerYear<Smlouva.Statistics.Data> CachedStatistics(Firma firma, int? obor, bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _smlouvaCache.Delete((firma, obor));

            return _smlouvaCache.Get((firma, obor));
        }
        private static async Task<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> _calculateSmlouvyStatsAsync(Firma f, int? obor)
        {
            StatisticsSubjectPerYear<Smlouva.Statistics.Data> res = null;
            if (obor.HasValue && obor != 0)
                res = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                    f.ICO,
                    await SmlouvaStatistics.CalculateAsync($"ico:{f.ICO} AND oblast:{Smlouva.SClassification.Classification.ClassifSearchQuery(obor.Value)}")
                );
            else
                res = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                    f.ICO,
                    await SmlouvaStatistics.CalculateAsync($"ico:{f.ICO}")
                );

            return res;
        }


    }
}