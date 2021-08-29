using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities.Analysis;
using HlidacStatu.Lib.Analytics;

using System;
using System.Collections.Generic;

namespace HlidacStatu.Repositories.Statistics
{
    public static class SmlouvaStatistics
    {
        static Util.Cache.CouchbaseCacheManager<StatisticsPerYear<Smlouva.Statistics.Data>, string> _cache
            = Util.Cache.CouchbaseCacheManager<StatisticsPerYear<Smlouva.Statistics.Data>, string>
                .GetSafeInstance("SmlouvyStatistics_Query_v3_",
                    (query) => Calculate(query),
                    TimeSpan.FromHours(12),
                    Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                    Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                    Devmasters.Config.GetWebConfigValue("CouchbasePassword"));

        static object _cachesLock = new object();


        public static StatisticsPerYear<Smlouva.Statistics.Data> CachedStatisticsForQuery(string query)
        {
            return _cache.Get(query);
        }


        public static StatisticsPerYear<Smlouva.Statistics.Data> Calculate(string query)
        {
            Dictionary<int, BasicData> _calc_SeZasadnimNedostatkem =
                ES.QueryGrouped.SmlouvyPerYear($"({query}) AND chyby:zasadni", Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_UzavrenoOVikendu =
                ES.QueryGrouped.SmlouvyPerYear($"({query}) AND (hint.denUzavreni:>0)",
                    Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_ULimitu =
                ES.QueryGrouped.SmlouvyPerYear($"({query}) AND ( hint.smlouvaULimitu:>0 )",
                    Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_NovaFirmaDodavatel =
                ES.QueryGrouped.SmlouvyPerYear(
                    $"({query}) AND ( hint.pocetDniOdZalozeniFirmy:>-50 AND hint.pocetDniOdZalozeniFirmy:<30 )",
                    Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_smlouvy =
                ES.QueryGrouped.SmlouvyPerYear($"({query}) ", Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_bezCeny =
                ES.QueryGrouped.SmlouvyPerYear($"({query}) AND ( hint.skrytaCena:1 ) ",
                    Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_bezSmlStran =
                ES.QueryGrouped.SmlouvyPerYear($"({query}) AND ( issues.issueTypeId:18 OR issues.issueTypeId:12 ) ",
                    Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_sVazbouNaPolitikyNedavne =
                ES.QueryGrouped.SmlouvyPerYear($"({query}) AND ( sVazbouNaPolitikyNedavne:true ) ",
                    Consts.RegistrSmluvYearsList);
            Dictionary<int, BasicData> _calc_sVazbouNaPolitikyBezCenyNedavne =
                ES.QueryGrouped.SmlouvyPerYear(
                    $"({query}) AND ( hint.skrytaCena:1 ) AND ( sVazbouNaPolitikyNedavne:true ) ",
                    Consts.RegistrSmluvYearsList);

            Dictionary<int, BasicData> _calc_soukrome =
                ES.QueryGrouped.SmlouvyPerYear($"({query}) AND ( hint.vztahSeSoukromymSubjektem:>0 ) ",
                    Consts.RegistrSmluvYearsList);
            Dictionary<int, BasicData> _calc_soukromeBezCeny =
                ES.QueryGrouped.SmlouvyPerYear(
                    $"({query}) AND ( hint.skrytaCena:1 ) AND ( hint.vztahSeSoukromymSubjektem:>0 ) ",
                    Consts.RegistrSmluvYearsList);
            //ES.QueryGrouped.SmlouvyPerYear($"({query}) AND ( issues.skrytaCena:1 ) AND ( hint.vztahSeSoukromymSubjektem:>0 ) ", Consts.RegistrSmluvYearsList);

            var _calc_poOblastech =
                ES.QueryGrouped.OblastiPerYear($"( {query} )", Consts.RegistrSmluvYearsList);

            Dictionary<int, Smlouva.Statistics.Data> data = new Dictionary<int, Smlouva.Statistics.Data>();
            foreach (var year in Consts.RegistrSmluvYearsList)
            {
                data.Add(year, new Smlouva.Statistics.Data()
                {
                    PocetSmluv = _calc_smlouvy[year].Pocet,
                    CelkovaHodnotaSmluv = _calc_smlouvy[year].CelkemCena,
                    PocetSmluvBezCeny = _calc_bezCeny[year].Pocet,
                    PocetSmluvBezSmluvniStrany = _calc_bezSmlStran[year].Pocet,
                    SumKcSmluvBezSmluvniStrany = _calc_bezSmlStran[year].CelkemCena,
                    PocetSmluvSeSoukromymSubj = _calc_soukrome[year].Pocet,
                    CelkovaHodnotaSmluvSeSoukrSubj = _calc_soukrome[year].CelkemCena,
                    PocetSmluvBezCenySeSoukrSubj = _calc_soukromeBezCeny[year].Pocet,

                    PocetSmluvSponzorujiciFirmy = _calc_sVazbouNaPolitikyNedavne[year].Pocet,
                    PocetSmluvBezCenySponzorujiciFirmy = _calc_sVazbouNaPolitikyBezCenyNedavne[year].Pocet,
                    SumKcSmluvSponzorujiciFirmy = _calc_sVazbouNaPolitikyNedavne[year].CelkemCena,
                    PocetSmluvULimitu = _calc_ULimitu[year].Pocet,
                    PocetSmluvOVikendu = _calc_UzavrenoOVikendu[year].Pocet,
                    PocetSmluvSeZasadnimNedostatkem = _calc_SeZasadnimNedostatkem[year].Pocet,
                    PocetSmluvNovaFirma = _calc_NovaFirmaDodavatel[year].Pocet,
                    PoOblastech = _calc_poOblastech[year]
                }
                );
            }

            return new StatisticsPerYear<Smlouva.Statistics.Data>(data);
        }
    }
}