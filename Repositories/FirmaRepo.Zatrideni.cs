using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Devmasters;

using HlidacStatu.Connectors;
using HlidacStatu.Connectors.External;
using HlidacStatu.Entities;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Data.External.DatoveSchranky;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Util;
using HlidacStatu.Util.Cache;


namespace HlidacStatu.Repositories
{
    public static partial class FirmaRepo
    {
        public static class Zatrideni
        {
            private static CouchbaseCacheManager<Firma.Zatrideni.Item[], Firma.Zatrideni.StatniOrganizaceObor> instanceByZatrideni
                = CouchbaseCacheManager<Firma.Zatrideni.Item[], Firma.Zatrideni.StatniOrganizaceObor>.GetSafeInstance("oboryByObor", GetSubjektyDirect,
                    TimeSpan.FromDays(2),
                    Devmasters.Config.GetWebConfigValue("CouchbaseServers").Split(','),
                    Devmasters.Config.GetWebConfigValue("CouchbaseBucket"),
                    Devmasters.Config.GetWebConfigValue("CouchbaseUsername"),
                    Devmasters.Config.GetWebConfigValue("CouchbasePassword")
                );
            private static Firma.Zatrideni.Item[] GetSubjektyDirect(Firma.Zatrideni.StatniOrganizaceObor obor)
            {
                string[] icos = null;
                string sql = "";
                switch (obor)
                {
                    case Firma.Zatrideni.StatniOrganizaceObor.Vse:
                        icos = GetAllSubjektyFromRPP();
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Vsechny_ustredni_organy_statni_spravy:
                        icos = GetSubjektyFromRPP((int)Firma.Zatrideni.StatniOrganizaceObor.Dalsi_ustredni_organy_statni_spravy)
                            .Concat(GetSubjektyFromRPP((int)Firma.Zatrideni.StatniOrganizaceObor.Ministerstva))
                            .ToArray();
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Nemocnice:
                        sql = @"select distinct ico from (
                                    select f.ICO from Firma f where Jmeno like N'%nemocnice%' and f.IsInRS = 1
                                    union
                                    select distinct f.ico
                                        from Firma_NACE fn
                                        join firma f on f.ICO = fn.ICO
                                        where (nace like '861%' or NACE like '862%') and f.IsInRS = 1
                                        and f.Kod_PF not in (105, 101, 801, 601)
        								                            
                                    ) as f
                                    where f.ICO not in ('70876606','70994226','45274649','05243793','64203450','25916092','60800691','08297517','00212423')";
                        icos = GetSubjektyFromSQL(sql)
                            .Append("00023752")
                            .ToArray();
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Velke_nemocnice:
                        icos = "00064165,00064173,00064203,00098892,00159816,00179906,00669806,00843989,25488627,26365804,27283933,27661989,65269705,27283518,26000202,00023736,00023884,27256391,61383082,27256537,00023001,27520536,26068877,47813750,00064211,00209805,27660915,00635162,27256456,00090638,00092584,00064190"
                            .Split(',');
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Zakladni_a_stredni_skoly:
                        icos = "48132306,61388530,14891212,72547651,61388262,61986038,69457930,69456330,47515601,60552255,47611863,70882452,14891239,69781745,64422402,16977246,71294520,49518925,49558277,12907731,60064781,72744057,60153296,61388483,00582158,70599921,00638013,60061880,14450470,61386855,66610702,48135445,49518933,71294775,14451026,00566756,00072583,48895598,13644319,00510874,60061863,70872589,00300268,48135453,14891522,75137011,25916092,68783728,00511382,15530213,64122654,00577260,75075920,62690361"
                            .Split(',');
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Knihovny:
                        icos = "00064467,00094943,00078077,00023221,61387142,62951491"
                            .Split(',');
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Muzea_a_galerie:
                        icos = "00023311,00100595,00228745,75075741,00094927,00023272,00083232,00101435,00305847,00096296,72053801,00098604,00072150,00023442,00095630,00079481,00073539,00072486,14450542,00023299,00064432,00089613,00089982,00094862,00069850,00368563,75079950,00091138,00094927,00023281,00368563,00263338,00069922,00094871,00177270,00073512"
                            .Split(',');
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Kulturni_a_kongresova_centra:
                        icos = "00320463,47308095,44557141,00076686,63080249,69092150"
                            .Split(',');
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Divadla:
                        icos = "00023205,00064297,00094820,00673692,00023337,00101397,00078051,00100544,00100528,00064335"
                            .Split(',');
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.ZOO:
                        icos = "00404454,00379719,00377015,00079651,27478246,00064459,00373249,00101451,00090026"
                            .Split(',');
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Statni_podniky:
                        icos = "46355901,70898219,25702556,03630919,49277600,00001279,70890005,72029455,70889953,70889988,14450216,60193468,04095316,29372259,70994234,70890021,24729035,05800226,00002739,04767543,00007536,07460121,45249130,48133990,49710371,70994226,02795281,00311391,28244532,45274649,60193531,25291581,47114983"
                            .Split(',');
                        sql = @"select ico from Firma f where f.kod_pf in (302,301) and f.IsInRs = 1";
                        icos = GetSubjektyFromSQL(sql).Union(icos).Distinct().ToArray();

                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Agentury:
                        icos = "62933591,00001171,71377999,72050365,75123924,06578705,07460121,45249130"
                            .Split(',');
                        break;

                    case Firma.Zatrideni.StatniOrganizaceObor.Fakultni_nemocnice:
                        icos = "65269705,00179906,00669806,00098892,00843989,00064165,00064203,00159816,00064173"
                            .Split(',');
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Akademicke_instituce:
                        icos = "86652052,00020702,68378271,68378033,61389030,61389005,86652036,67985912,68378041,86652079,67985939,61388955,67985998,00027073,67985955,60077344,00027014,67985882,68081731,00027006,61389013,61389021,00020711,68378289,61388971,68081723,60457856,67985530,68081707,67985858,68378050,68081758,67985904,44994575,29372259,61388963,68378025,67985891,67985823,61388998,68145535,00027162,00010669"
                            .Split(',');
                        sql = @"select ico from Firma f where f.kod_pf in (661) and f.IsInRs = 1";
                        icos = GetSubjektyFromSQL(sql).Union(icos).Distinct().ToArray();
                        break;

                    case Firma.Zatrideni.StatniOrganizaceObor.Krajske_spravy_silnic:
                        icos = "00066001,00090450,70947023,70946078,72053119,00080837,70971641,27502988,00085031,70932581,70960399,00095711,26913453,03447286,25396544,60733098"
                            .Split(',');
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Dopravni_podniky:
                        icos = "05792291,25095251,25136046,25137280,00005886,25166115,25164538,25220683,29099846,61058238,48364282,62240935,64053466,06231292,62242504,25013891,47311975,00079642,06873031,25267213,63217066,25512897,25508881,00100790,47676639,05724252,64610250,61974757,60730153"
                            .Split(',');
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Technicke_sluzby:
                        sql = @"select ico from Firma f where Jmeno like N'technické služby%' and f.IsInRS = 1";
                        icos = GetSubjektyFromSQL(sql);
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Domov_duchodcu:
                        sql = @"select ico from Firma f where Jmeno like N'%domov důchodců%' and f.IsInRs = 1";
                        icos = GetSubjektyFromSQL(sql);
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Vyjimky_RS:

                        // Poslanecká sněmovna, Senát, Kancelář prezidenta republiky, Ústavní soud, Nejvyšší kontrolní úřad, 
                        //Kancelář veřejného ochránce práv a Úřad Národní rozpočtové rady
                        //CNB
                        icos = new string[] { "00006572", "63839407", "48136000", "48513687", "49370227", "70836981", "05553539", "48136450" };
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Ostatni:
                        icos = new string[] { };
                        break;
                    case Firma.Zatrideni.StatniOrganizaceObor.Media:
                        sql = @"select ico from Firma f where f.kod_pf in (361,362) and f.IsInRs = 1";
                        icos = GetSubjektyFromSQL(sql);
                        break;
                    default:
                        icos = GetSubjektyFromRPP((int)obor);
                        break;
                }
                bool removeKraj = false;
                switch (obor)
                {
                    case Firma.Zatrideni.StatniOrganizaceObor.Ostatni:
                    case Firma.Zatrideni.StatniOrganizaceObor.Zdravotni_pojistovny:
                    case Firma.Zatrideni.StatniOrganizaceObor.Fakultni_nemocnice:
                    case Firma.Zatrideni.StatniOrganizaceObor.Krajska_statni_zastupitelstvi:
                    case Firma.Zatrideni.StatniOrganizaceObor.Krajske_soudy:
                    case Firma.Zatrideni.StatniOrganizaceObor.Kraje_Praha:
                    case Firma.Zatrideni.StatniOrganizaceObor.Mestske_casti_Prahy:
                    case Firma.Zatrideni.StatniOrganizaceObor.OSSZ:
                    case Firma.Zatrideni.StatniOrganizaceObor.Ministerstva:
                    case Firma.Zatrideni.StatniOrganizaceObor.Organizacni_slozky_statu:
                    case Firma.Zatrideni.StatniOrganizaceObor.Vsechny_ustredni_organy_statni_spravy:
                    case Firma.Zatrideni.StatniOrganizaceObor.Dalsi_ustredni_organy_statni_spravy:
                    case Firma.Zatrideni.StatniOrganizaceObor.Financni_urady:
                    case Firma.Zatrideni.StatniOrganizaceObor.Vyjimky_RS:
                    case Firma.Zatrideni.StatniOrganizaceObor.Verejne_vysoke_skoly:
                    case Firma.Zatrideni.StatniOrganizaceObor.Konzervatore:
                    case Firma.Zatrideni.StatniOrganizaceObor.Krajske_spravy_silnic:
                    case Firma.Zatrideni.StatniOrganizaceObor.OVM_pro_evidenci_skutecnych_majitelu:
                        removeKraj = true;
                        break;
                    default:
                        break;
                }

                if (icos.Count() == 0)
                    return new Firma.Zatrideni.Item[] { };
                else
                {
                    var ret = new System.Collections.Generic.List<Firma.Zatrideni.Item>();
                    Devmasters.Batch.Manager.DoActionForAll<string>(icos.Select(m => m.Trim()).Distinct(),
                        ic =>
                        {
                            var f = Firmy.Get(ic);

                            if (f.PatrimStatu())
                            {
                                lock (_getSubjektyDirectLock)
                                {
                                    if (!ret.Any(ff => ff.Ico == f.ICO))
                                    {
                                        ret.Add(new Firma.Zatrideni.Item()
                                        {
                                            Ico = f.ICO,
                                            Jmeno = f.Jmeno,
                                            KrajId = removeKraj ? "" : f.KrajId,
                                            Kraj = removeKraj ? "" : CZ_Nuts.Nace2Kraj(f.KrajId, "(neznamý)")
                                        });
                                    }
                                }
                            }
                            return new Devmasters.Batch.ActionOutputData();
                        }, !System.Diagnostics.Debugger.IsAttached);

                    return ret.ToArray();
                }
            }
            private static string[] GetSubjektyFromSQL(string sql)
            {
                return DirectDB
                    .GetList<string>(sql)
                    .ToArray();
            }

            private static string[] GetSubjektyFromRPP(int rpp_kategorie_id)
            {
                var res = Manager.GetESClient_RPP_Kategorie()
                    .Get<Lib.Data.External.RPP.KategorieOVM>(rpp_kategorie_id.ToString());
                if (res.Found)
                    return res.Source.OVM_v_kategorii.Select(m => m.kodOvm).ToArray();

                return new string[] { };
            }

            private static string[] GetAllSubjektyFromRPP()
            {
                var res = Manager.GetESClient_RPP_Kategorie()
                    .Search<Lib.Data.External.RPP.KategorieOVM>(s => s.Size(9000).Query(q => q.MatchAll()));
                if (res.IsValid)
                    return res.Hits
                        .Select(m => m.Source)
                        .SelectMany(m => m.OVM_v_kategorii.Select(ovm => ovm.kodOvm))
                        .Distinct()
                        .ToArray();

                return new string[] { };
            }

            public static Firma.Zatrideni.Item[] Subjekty(Firma.Zatrideni.StatniOrganizaceObor obor)
            {
                return instanceByZatrideni.Get(obor);
            }


            static object _getSubjektyDirectLock = new object();
        }
    }
}