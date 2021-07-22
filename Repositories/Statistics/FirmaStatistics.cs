using System;
using System.Collections.Generic;
using System.Linq;

using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities.Analysis;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;

using Nest;

namespace HlidacStatu.Repositories.Statistics
{
    public static class FirmaStatistics
    {
        internal static Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, Firma> DotaceCache()
        {
            var cache = Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, Firma>
                .GetSafeInstance("Firma_DotaceStatistics",
                    (firma) => CreateDotace(firma),
                    TimeSpan.FromHours(12),
                    Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                    Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                    Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                    f => f.ICO);

            return cache;
        }
        internal static Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, (Firma firma, Datastructures.Graphs.Relation.AktualnostType aktualnost)> HoldingDotaceCache()
        {
            var cache = Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Firma.Statistics.Dotace>, (Firma firma, Datastructures.Graphs.Relation.AktualnostType aktualnost)>
                .GetSafeInstance("Holding_DotaceStatistics",
                    (obj) => CreateHoldingDotace(obj.firma, obj.aktualnost),
                    TimeSpan.FromHours(12),
                    Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                    Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                    Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                    obj => obj.firma.ICO + "-" + obj.aktualnost.ToString());

            return cache;
        }

        public static StatisticsSubjectPerYear<Firma.Statistics.Dotace> CreateHoldingDotace(Firma firma, Datastructures.Graphs.Relation.AktualnostType aktualnost)
        {
            var firmy = firma.Holding(aktualnost).ToArray();

            var statistiky = firmy.Select(f => f.StatistikaDotaci()).Append(firma.StatistikaDotaci()).ToArray();

            var aggregate = Lib.Analytics.StatisticsSubjectPerYear<Firma.Statistics.Dotace>.Aggregate(statistiky);

            return aggregate;

        }

        public static StatisticsSubjectPerYear<Firma.Statistics.Dotace> CreateDotace(Firma f)
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

        static Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, Datastructures.Graphs.Relation.AktualnostType aktualnost, int? obor)> _holdingSmlouvaCache
            = Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, Datastructures.Graphs.Relation.AktualnostType aktualnost, int? obor)>
                .GetSafeInstance("Holding_SmlouvyStatistics_v3_",
                    (obj) => CalculateStats(obj.firma, obj.obor),
                    TimeSpan.FromHours(12),
                    Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                    Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                    Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                    obj => obj.firma.ICO + "-" + obj.aktualnost.ToString() + "-" + (obj.obor ?? 0));

        public static StatisticsSubjectPerYear<Smlouva.Statistics.Data> HoldingCachedStatistics(Firma firma, Datastructures.Graphs.Relation.AktualnostType aktualnost, int? obor = null)
        {
            return _holdingSmlouvaCache.Get((firma, aktualnost, obor));
        }
        public static StatisticsSubjectPerYear<Smlouva.Statistics.Data> HoldingCalculateStats(Firma f, Datastructures.Graphs.Relation.AktualnostType aktualnost, int? obor)
        {
            var firmy = f.Holding(aktualnost).ToArray();

            var statistiky = firmy.Select(f => f.StatistikaRegistruSmluv(obor)).Append(f.StatistikaRegistruSmluv(obor))
                .ToArray();

            var aggregate = Lib.Analytics.StatisticsSubjectPerYear<Smlouva.Statistics.Data>.Aggregate(statistiky);

            return aggregate;

        }

        static Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, int? obor)> _smlouvaCache
            = Util.Cache.CouchbaseCacheManager<StatisticsSubjectPerYear<Smlouva.Statistics.Data>, (Firma firma, int? obor)>
                .GetSafeInstance("Firma_SmlouvyStatistics_v3_",
                    (obj) => CalculateStats(obj.firma, obj.obor),
                    TimeSpan.FromHours(12),
                    Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                    Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                    Devmasters.Config.GetWebConfigValue("CouchbasePassword"),
                    obj => obj.firma.ICO + "-" + (obj.obor ?? 0));

        public static StatisticsSubjectPerYear<Smlouva.Statistics.Data> CachedStatistics(Firma firma, int? obor)
        {
            return _smlouvaCache.Get((firma, obor));
        }
        public static StatisticsSubjectPerYear<Smlouva.Statistics.Data> CalculateStats(Firma f, int? obor)
        {
            StatisticsSubjectPerYear<Smlouva.Statistics.Data> res = null;
            if (obor.HasValue && obor != 0)
                res = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                    f.ICO,
                    SmlouvaStatistics.Calculate($"ico:{f.ICO} AND oblast:{Smlouva.SClassification.Classification.ClassifSearchQuery(obor.Value)}")
                );
            else
                res = new StatisticsSubjectPerYear<Smlouva.Statistics.Data>(
                    f.ICO,
                    SmlouvaStatistics.Calculate($"ico:{f.ICO}")
                );

            return res;
        }

        private static Util.Cache.CouchbaseCacheManager<Firma.Statistics.VZ, string> _vzCache
            = Util.Cache.CouchbaseCacheManager<Firma.Statistics.VZ, string>.GetSafeInstance("Firma_SmlouvyStatistics", CreateVZ, TimeSpan.FromHours(12),
                Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                Devmasters.Config.GetWebConfigValue("CouchbasePassword"));

        public static Firma.Statistics.VZ GetVZ(string ico)
        {
            return _vzCache.Get(ico);

        }
        public static Firma.Statistics.VZ GetVZ(Firma f)
        {
            //add cache logic

            return _vzCache.Get(f.ICO);
        }
        private static Firma.Statistics.VZ CreateVZ(string ico)
        {
            Firma f = Firmy.Get(ico);
            if (f.Valid == false)
                return new Firma.Statistics.VZ() { ICO = "--------" };
            else
                return Create(f);
        }
        private static Firma.Statistics.VZ Create(Firma f)
        {

            Dictionary<int, BasicData> _calc_SeZasadnimNedostatkem = ES.QueryGrouped.SmlouvyPerYear($"ico:{f.ICO} and chyby:zasadni", Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_UzavrenoOVikendu = ES.QueryGrouped.SmlouvyPerYear($"ico:{f.ICO} AND (hint.denUzavreni:>0)", Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_ULimitu = ES.QueryGrouped.SmlouvyPerYear($"ico:{f.ICO} AND ( hint.smlouvaULimitu:>0 )", Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_NovaFirmaDodavatel = ES.QueryGrouped.SmlouvyPerYear($"ico:{f.ICO} AND ( hint.pocetDniOdZalozeniFirmy:>-50 AND hint.pocetDniOdZalozeniFirmy:<30 )", Consts.RegistrSmluvYearsList);


            Dictionary<int, Firma.Statistics.VZ.Data> data = new();
            foreach (var year in Consts.RegistrSmluvYearsList)
            {
                //var stat = f.Statistic().RatingPerYear[year];
                data.Add(year, new Firma.Statistics.VZ.Data()
                {
                }
                );
            }

            return new Firma.Statistics.VZ(f.ICO, data);
        }



    }
}