using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Repositories.Statistics
{
    public static class OsobaStatistics
    {
        static Devmasters.Cache.Redis.Manager<Osoba.Statistics.RegistrSmluv, (Osoba os, int aktualnost, int? obor)>
            _cacheSmlouvy
                = Devmasters.Cache.Redis.Manager<Osoba.Statistics.RegistrSmluv, (Osoba os, int aktualnost, int? obor)>
                    .GetSafeInstance("Osoba_SmlouvyStatistics_v1_",
                        (obj) => CalculateSmlouvyStat(obj.os, (Relation.AktualnostType)obj.aktualnost, obj.obor),
                        TimeSpan.Zero,
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                        keyValueSelector: obj => $"{obj.os.NameId}/{obj.aktualnost}/{(obj.obor ?? 0)}");

        static Devmasters.Cache.Redis.Manager<Osoba.Statistics.Dotace, (Osoba os, int aktualnost)>
      _cacheDotace
          = Devmasters.Cache.Redis.Manager<Osoba.Statistics.Dotace, (Osoba os, int aktualnost)>
              .GetSafeInstance("Osoba_DotaceStatistics_v1_",
                  (obj) => CalculateDotaceStat(obj.os, (Relation.AktualnostType)obj.aktualnost),
                  TimeSpan.Zero,
                    Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                    Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                    Devmasters.Config.GetWebConfigValue("RedisUsername"),
                    Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                  keyValueSelector: obj => $"{obj.os.NameId}/{obj.aktualnost}");


        public static void RemoveCachedStatistics_Smlouvy(Osoba os, int? obor)
        {
            _cacheSmlouvy.Delete((os, (int)Relation.AktualnostType.Aktualni, obor));
            _cacheSmlouvy.Delete((os, (int)Relation.AktualnostType.Libovolny, obor));
            _cacheSmlouvy.Delete((os, (int)Relation.AktualnostType.Neaktualni, obor));
            _cacheSmlouvy.Delete((os, (int)Relation.AktualnostType.Nedavny, obor));

        }

        public static void RemoveCachedStatistics_Dotace(Osoba os)
        {
            _cacheDotace.Delete((os, (int)Relation.AktualnostType.Aktualni));

        }


        public static Osoba.Statistics.RegistrSmluv CachedStatistics_Smlouvy(Osoba os, Relation.AktualnostType aktualnost,
            int? obor, bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _cacheSmlouvy.Delete((os, (int)aktualnost, obor));

            return _cacheSmlouvy.Get((os, (int)aktualnost, obor));
        }

        public static Osoba.Statistics.Dotace CachedStatistics_Dotace(Osoba os, Relation.AktualnostType aktualnost,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _cacheDotace.Delete((os, (int)aktualnost));

            return _cacheDotace.Get((os, (int)aktualnost));
        }

        public static Osoba.Statistics.RegistrSmluv CalculateSmlouvyStat(Osoba o, Relation.AktualnostType aktualnost, int? obor)
        {
            Osoba.Statistics.RegistrSmluv res = new Osoba.Statistics.RegistrSmluv();
            res.OsobaNameId = o.NameId;
            res.Aktualnost = aktualnost;
            res.Obor = (Smlouva.SClassification.ClassificationsTypes?)obor;

            Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>> statni =
                new Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>>();
            Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>> soukr =
                new Dictionary<string, StatisticsSubjectPerYear<Smlouva.Statistics.Data>>();

            var perIcoStat = o.AktualniVazby(aktualnost)
                .Where(v => !string.IsNullOrEmpty(v.To?.UniqId)
                            && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                .Select(v => v.To)
                .Distinct(new HlidacStatu.DS.Graphs.Graph.NodeComparer())
                .Select(f => Firmy.Get(f.Id))
                .Where(f => f.Valid == true)
                .Select(f => new { f = f, ss = f.StatistikaRegistruSmluv(obor) });


            foreach (var it in perIcoStat)
            {

                if (it.f.PatrimStatu())
                    statni.Add(it.f.ICO, it.ss);
                else
                    soukr.Add(it.f.ICO, it.ss);
            }

            res.StatniFirmy = statni;
            res.SoukromeFirmy = soukr;

            return res;
        }


        public static Osoba.Statistics.Dotace CalculateDotaceStat(Osoba o, Relation.AktualnostType aktualnost)
        {
            Osoba.Statistics.Dotace res = new Osoba.Statistics.Dotace();
            res.OsobaNameId = o.NameId;
            res.Aktualnost = aktualnost;

            Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> statni =
                new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();
            Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> soukr =
                new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();
            Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> nezisk =
                new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();

            var perIcoStat = o.AktualniVazby(aktualnost)
                .Where(v => !string.IsNullOrEmpty(v.To?.UniqId)
                            && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType.Company)
                .Select(v => v.To)
                .Distinct(new HlidacStatu.DS.Graphs.Graph.NodeComparer())
                .Select(f => Firmy.Get(f.Id))
                .Where(f => f.Valid == true)
                .Select(f => new { f = f, ss = f.StatistikaDotaci() });


            foreach (var it in perIcoStat)
            {

                if (it.f.PatrimStatu())
                    statni.Add(it.f.ICO, it.ss);
                if (it.f.JsemNeziskovka())
                    nezisk.Add(it.f.ICO, it.ss);
                else
                    soukr.Add(it.f.ICO, it.ss);
            }

            res.StatniFirmy = statni;
            res.SoukromeFirmy = soukr;
            res.Neziskovky = nezisk;

            return res;
        }

        public static string[] SmlouvyStat_Neziskovky(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._neziskovkyIcos == null)
            {
                registrSmluv._neziskovkyIcos = registrSmluv.SoukromeFirmy
                    .Select(m => Firmy.Get(m.Key))
                    .Where(ff => ff.JsemNeziskovka())
                    .Select(s => s.ICO)
                    .ToArray();
            }

            return registrSmluv._neziskovkyIcos;
        }

        public static int SmlouvyStat_NeziskovkyCount(this Osoba.Statistics.RegistrSmluv registrSmluv) => registrSmluv.SmlouvyStat_Neziskovky().Count();

        public static int SmlouvyStat_KomercniFirmyCount(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            return registrSmluv.SoukromeFirmy.Count - registrSmluv.SmlouvyStat_NeziskovkyCount();
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> SmlouvyStat_SoukromeFirmySummary(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._soukromeFirmySummary == null)
                registrSmluv._soukromeFirmySummary = registrSmluv.SoukromeFirmy?.Values.AggregateStats();

            return registrSmluv._soukromeFirmySummary;
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> SmlouvyStat_StatniFirmySummary(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._statniFirmySummary == null)
                registrSmluv._statniFirmySummary = registrSmluv.StatniFirmy?.Values.AggregateStats();

            return registrSmluv._statniFirmySummary;
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> SmlouvyStat_NeziskovkySummary(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._neziskovkySummary == null)
                registrSmluv._neziskovkySummary = registrSmluv.SoukromeFirmy?
                    .Where(k => registrSmluv.SmlouvyStat_Neziskovky().Contains(k.Key))
                    .Select(m => m.Value)
                    .AggregateStats();

            return registrSmluv._neziskovkySummary;
        }
    }
}