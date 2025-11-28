using Devmasters.DT;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Analytics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Repositories.Cache;

namespace HlidacStatu.Repositories.Statistics
{
    public static class OsobaStatistics
    {
        static Devmasters.Cache.Redis.Manager<Osoba.Statistics.Dotace, Osoba>
            _cacheDotace
                = Devmasters.Cache.Redis.Manager<Osoba.Statistics.Dotace, Osoba>
                    .GetSafeInstance("Osoba_DotaceStatistics_v1_",
                        (os) => CalculateDotaceStat(os),
                        TimeSpan.Zero,
                        Devmasters.Config.GetWebConfigValue("RedisServerUrls").Split(';'),
                        Devmasters.Config.GetWebConfigValue("RedisBucketName"),
                        Devmasters.Config.GetWebConfigValue("RedisUsername"),
                        Devmasters.Config.GetWebConfigValue("RedisCachePassword"),
                        keyValueSelector: os => $"{os.NameId}");
        

        public static void RemoveCachedStatistics_Dotace(Osoba os)
        {
            _cacheDotace.Delete(os);
        }


        public static async Task<Osoba.Statistics.RegistrSmluv> CachedStatistics_SmlouvyAsync(Osoba os,
            int? obor, bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                await StatisticsCache.InvalidateOsobaSmlouvyStatisticsAsync(os, obor);

            return await StatisticsCache.GetOsobaSmlouvyStatisticsAsync(os, obor);
        }

        public static Osoba.Statistics.Dotace CachedStatistics_Dotace(Osoba os,
            bool forceUpdateCache = false)
        {
            if (forceUpdateCache)
                _cacheDotace.Delete(os);

            return _cacheDotace.Get(os);
        }


        private static Osoba.Statistics.Dotace CalculateDotaceStat(Osoba o)
        {
            Osoba.Statistics.Dotace res = new Osoba.Statistics.Dotace();
            res.OsobaNameId = o.NameId;

            Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> statni =
                new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();
            Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> soukr =
                new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();
            Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>> nezisk =
                new Dictionary<string, StatisticsSubjectPerYear<Firma.Statistics.Dotace>>();

            var skutecneVazby = Relation.SkutecnaDobaVazby(o.AktualniVazby(Relation.AktualnostType.Libovolny));
            var firmy_maxrok = new Dictionary<string, DateInterval>();
            foreach (var v in skutecneVazby.Where(v => !string.IsNullOrEmpty(v.To?.UniqId)
                                                       && v.To.Type == HlidacStatu.DS.Graphs.Graph.Node.NodeType
                                                           .Company)
                    )
            {
                firmy_maxrok.TryAdd(v.To.Id,
                    new DateInterval(v.RelFrom ?? DateTime.MinValue, v.RelTo ?? DateTime.MaxValue));
            }


            var perIcoStat = firmy_maxrok
                .Select(m => new { firma = Firmy.Get(m.Key), interval = m.Value })
                .Where(m => m.firma.Valid == true)
                .Select(m => new
                {
                    f = m.firma,
                    ss = new StatisticsSubjectPerYear<Firma.Statistics.Dotace>(
                        m.firma.ICO,
                        m.firma.StatistikaDotaci().Filter(fi =>
                            fi.Key >= m.interval.From?.Year && fi.Key <= m.interval.To?.Year)
                    )
                });


            foreach (var fStat in perIcoStat)
            {
                if (fStat.f.PatrimStatu() && statni.ContainsKey(fStat.f.ICO) == false)
                    statni.Add(fStat.f.ICO, fStat.ss);
                else if (fStat.f.JsemNeziskovka() && nezisk.ContainsKey(fStat.f.ICO) == false)
                    nezisk.Add(fStat.f.ICO, fStat.ss);
                else if (soukr.ContainsKey(fStat.f.ICO) == false)
                    soukr.Add(fStat.f.ICO, fStat.ss);
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

        public static int SmlouvyStat_NeziskovkyCount(this Osoba.Statistics.RegistrSmluv registrSmluv) =>
            registrSmluv.SmlouvyStat_Neziskovky().Count();

        public static int SmlouvyStat_KomercniFirmyCount(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            return registrSmluv.SoukromeFirmy.Count - registrSmluv.SmlouvyStat_NeziskovkyCount();
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> SmlouvyStat_SoukromeFirmySummary(
            this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._soukromeFirmySummary == null)
                registrSmluv._soukromeFirmySummary = registrSmluv.SoukromeFirmy?.Values.AggregateStats();

            return registrSmluv._soukromeFirmySummary;
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> SmlouvyStat_StatniFirmySummary(
            this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._statniFirmySummary == null)
                registrSmluv._statniFirmySummary = registrSmluv.StatniFirmy?.Values.AggregateStats();

            return registrSmluv._statniFirmySummary;
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> SmlouvyStat_NeziskovkySummary(
            this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._neziskovkySummary == null)
                registrSmluv._neziskovkySummary = registrSmluv.Neziskovky?.Values.AggregateStats();

            return registrSmluv._neziskovkySummary;
        }
    }
}