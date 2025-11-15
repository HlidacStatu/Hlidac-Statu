using Devmasters.DT;
using HlidacStatu.Connectors;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HlidacStatu.Repositories.Statistics
{
    public static partial class FirmaStatistics
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(FirmaStatistics));
        private static readonly string _version = "v4a";

        static Devmasters.Cache.Redis.Manager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma,  int? obor)>
            _holdingSmlouvaCache
            = Devmasters.Cache.Redis.Manager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, int? obor)>
                .GetSafeInstance($"Holding_SmlouvyStatistics_{_version}_",
                    (obj) => _holdingCalculateStats(obj.firma, obj.obor),
                    TimeSpan.Zero,
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                    keyValueSelector: obj => obj.firma.ICO + "-" + (obj.obor ?? 0));

        public static StatisticsSubjectPerYear<Smlouva.Statistics.Data> CachedHoldingStatisticsSmlouvy(
            Firma firma, int? obor = null,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _holdingSmlouvaCache.Delete((firma, obor));

                return _holdingSmlouvaCache.Get((firma, obor));
        }

        private static StatisticsSubjectPerYear<Smlouva.Statistics.Data> _holdingCalculateStats(
            Firma f, int? obor)
        {

            var firmy_maxrok = new Dictionary<string, DateInterval>();
            firmy_maxrok.Add(f.ICO, new DateInterval(DateTime.MinValue, DateTime.MaxValue));
            var skutecneVazby = Relation.SkutecnaDobaVazby(f.AktualniVazby(DS.Graphs.Relation.AktualnostType.Libovolny));
            foreach (var v in skutecneVazby)
            {
                if (!string.IsNullOrEmpty(v.To?.UniqId)
                            && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                {
                    firmy_maxrok.TryAdd(v.To.Id, new DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
                }

            }
            var statistiky = firmy_maxrok
                .Select(f => new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                                f.Key,
                                Firmy.Get(f.Key).StatistikaRegistruSmluv(obor).Filter(m => m.Key >= f.Value.From?.Year && m.Key <= f.Value.To?.Year)
                           )
                )
                .ToArray();
            if (statistiky.Length == 0)
                return new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(f.ICO);
            if (statistiky.Length == 1)
                return statistiky[0];

            StatisticsSubjectPerYear<Smlouva.Statistics.Data> aggregate =
                Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>.Aggregate(f.ICO, statistiky);

            return aggregate;

            /* old 
            var firmy = f.Holding(aktualnost).ToArray();

            var statistiky = firmy.Select(f => f.StatistikaRegistruSmluv(obor)).Append(f.StatistikaRegistruSmluv(obor))
                .ToArray();

            var aggregate = Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>.Aggregate(f.ICO,statistiky);

            return aggregate;
            */
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

            _holdingSmlouvaCache.Delete((firma, obor));
        }

        public static StatisticsSubjectPerYear<Smlouva.Statistics.Data> CachedStatisticsSmlouvy(Firma firma, int? obor, bool forceUpdateCache = false)
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