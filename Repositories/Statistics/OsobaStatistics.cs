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
        static Devmasters.Cache.Hazelcast.Manager<Osoba.Statistics.RegistrSmluv, (Osoba os, int aktualnost, int? obor)>
            _cache
                = Devmasters.Cache.Hazelcast.Manager<Osoba.Statistics.RegistrSmluv, (Osoba os, int aktualnost, int? obor)>
                    .GetSafeInstance("Osoba_SmlouvyStatistics_v1_",
                        (obj) => Calculate(obj.os, (Relation.AktualnostType)obj.aktualnost, obj.obor),
                        TimeSpan.FromHours(18),
                    Devmasters.Config.GetWebConfigValue("HazelcastServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("HazelcastClusterName"),
                    Devmasters.Config.GetWebConfigValue("HazelcastDbName"),
                    Devmasters.Config.GetWebConfigValue("HazelcastClientName"), 10000,
                        obj => $"{obj.os.NameId}/{obj.aktualnost}/{(obj.obor ?? 0)}");


        public static Osoba.Statistics.RegistrSmluv CachedStatistics(Osoba os, Relation.AktualnostType aktualnost,
            int? obor)
        {
            return _cache.Get((os, (int)aktualnost, obor));
        }


        public static Osoba.Statistics.RegistrSmluv Calculate(Osoba o, Relation.AktualnostType aktualnost, int? obor)
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

        public static string[] Neziskovky(this Osoba.Statistics.RegistrSmluv registrSmluv)
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

        public static int NeziskovkyCount(this Osoba.Statistics.RegistrSmluv registrSmluv) => registrSmluv.Neziskovky().Count();

        public static int KomercniFirmyCount(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            return registrSmluv.SoukromeFirmy.Count - registrSmluv.NeziskovkyCount();
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> SoukromeFirmySummary(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._soukromeFirmySummary == null)
                registrSmluv._soukromeFirmySummary = registrSmluv.SoukromeFirmy.Values.AggregateStats();

            return registrSmluv._soukromeFirmySummary;
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> StatniFirmySummary(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._statniFirmySummary == null)
                registrSmluv._statniFirmySummary = registrSmluv.StatniFirmy.Values.AggregateStats();

            return registrSmluv._statniFirmySummary;
        }

        public static StatisticsPerYear<Smlouva.Statistics.Data> NeziskovkySummary(this Osoba.Statistics.RegistrSmluv registrSmluv)
        {
            if (registrSmluv._neziskovkySummary == null)
                registrSmluv._neziskovkySummary = registrSmluv.SoukromeFirmy
                    .Where(k => registrSmluv.Neziskovky().Contains(k.Key))
                    .Select(m => m.Value)
                    .AggregateStats();

            return registrSmluv._neziskovkySummary;
        }
    }
}