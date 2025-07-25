using HlidacStatu.Connectors;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(FirmaStatistics));
        private static readonly string _version = "v4a";

        static Devmasters.Cache.Redis.Manager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost, int? obor)>
            _holdingSmlouvaCache
            = Devmasters.Cache.Redis.Manager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost, int? obor)>
                .GetSafeInstance($"Holding_SmlouvyStatistics_{_version}_",
                    (obj) => _holdingCalculateStats(obj.firma, obj.aktualnost, obj.obor),
                    TimeSpan.Zero,
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                    keyValueSelector: obj => obj.firma.ICO + "-" + obj.aktualnost.ToString() + "-" + (obj.obor ?? 0));

        public static StatisticsSubjectPerYear<Smlouva.Statistics.Data> CachedHoldingStatisticsSmlouvy(
            Firma firma, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost, int? obor = null,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _holdingSmlouvaCache.Delete((firma, aktualnost, obor));

                return _holdingSmlouvaCache.Get((firma, aktualnost, obor));
        }

        private static StatisticsSubjectPerYear<Smlouva.Statistics.Data> _holdingCalculateStats(
            Firma f, HlidacStatu.DS.Graphs.Relation.AktualnostType aktualnost, int? obor)
        {
            var firmy = f.Holding(aktualnost).ToArray();

            var statistiky = firmy.Select(f => f.StatistikaRegistruSmluv(obor)).Append(f.StatistikaRegistruSmluv(obor))
                .ToArray();

            var aggregate = Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>.Aggregate(f.ICO,statistiky);

            return aggregate;

        }

        static Devmasters.Cache.Redis.ManagerAsync<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, int? obor)>
            _smlouvaCache
            = Devmasters.Cache.Redis.ManagerAsync<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, int? obor)>
                .GetSafeInstance($"Firma_SmlouvyStatistics_{_version}_",
                    async (obj) => await _calculateSmlouvyStatsAsync(obj.firma, obj.obor),
                    TimeSpan.Zero,
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                    keyValueSelector: obj => obj.firma.ICO + "-" + (obj.obor ?? 0));

        public static void RemoveStatistics(Firma firma, int? obor)
        {
            _smlouvaCache.DeleteAsync((firma, obor)).ConfigureAwait(false).GetAwaiter().GetResult();

            _holdingSmlouvaCache.Delete((firma, HlidacStatu.DS.Graphs.Relation.AktualnostType.Aktualni, obor));
            _holdingSmlouvaCache.Delete((firma, HlidacStatu.DS.Graphs.Relation.AktualnostType.Nedavny, obor));
            _holdingSmlouvaCache.Delete((firma, HlidacStatu.DS.Graphs.Relation.AktualnostType.Libovolny, obor));
        }

        public static StatisticsSubjectPerYear<Smlouva.Statistics.Data> GetStatistics(Firma firma, int? obor, bool forceUpdateCache = false)
        {
            StatisticsSubjectPerYear<Smlouva.Statistics.Data> ret = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>();
            //STAT FIX
            //return ret;

            if (string.IsNullOrWhiteSpace(firma.ICO))
            {
                return ret;
            }
            /*            if (forceUpdateCache)
                            _ = Task.Run(async () => await _smlouvaCache.DeleteAsync((firma, obor))).Wait(TimeSpan.FromSeconds(10));

                        _ = Task.Run(async () => { ret = await _smlouvaCache.GetAsync((firma, obor)); }).Wait(TimeSpan.FromSeconds(20));
            */
            if (forceUpdateCache)
                _smlouvaCache.DeleteAsync((firma, obor)).ConfigureAwait(false).GetAwaiter().GetResult();

            ret = _smlouvaCache.GetAsync((firma, obor)).ConfigureAwait(false).GetAwaiter().GetResult();
            return ret;
        }
        public static void SetStatistics(Firma firma, int? obor, StatisticsSubjectPerYear<Smlouva.Statistics.Data> data)
        {
            _smlouvaCache.SetAsync((firma, obor), data).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        private static async Task<StatisticsSubjectPerYear<Smlouva.Statistics.Data>> _calculateSmlouvyStatsAsync(Firma f, int? obor)
        {
            StatisticsSubjectPerYear<Smlouva.Statistics.Data> res = null;
            if (obor.HasValue && obor != 0)
                res = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                    f.ICO,
                    await SmlouvyStatistics.CalculateAsync($"ico:{f.ICO} AND oblast:{Smlouva.SClassification.Classification.ClassifSearchQuery(obor.Value)}")
                );
            else
                res = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                    f.ICO,
                    await SmlouvyStatistics.CalculateAsync($"ico:{f.ICO}")
                );

            res = res ?? new StatisticsSubjectPerYear<Smlouva.Statistics.Data>();
            return res;
        }


    }
}