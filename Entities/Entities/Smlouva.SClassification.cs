﻿using Devmasters.Enums;

using System;
using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Entities
{
    public partial class Smlouva
    {
        public partial class SClassification
        {
            public class ClassifField
            {
                public ClassificationsTypes ClassifType { get; set; }
                public int Value { get; set; }
                public string SearchExpression { get; set; }
                public string SearchShortcut { get; set; }
                public string Fullname { get; set; }
                public bool IsMainType { get; set; }
                public int MainTypeValue { get; set; }
            }


            static object lockInit = new object();
            static SClassification()
            {
                lock (lockInit)
                {
                    if (AllTypes == null)
                    {
                        AllTypes = new List<ClassifField>();
                        var vals = Enum.GetValues(typeof(ClassificationsTypes))
                                as ClassificationsTypes[];

                        foreach (int v in vals)
                        {
                            ClassifField cf = new ClassifField();
                            cf.Value = v;
                            cf.ClassifType = (ClassificationsTypes)v;
                            cf.Fullname = cf.ClassifType.ToNiceDisplayName();

                            string sval = v.ToString();
                            var name = (ClassificationsTypes)v;
                            string sname = name.ToString().ToLower();

                            if (sname.EndsWith("_obecne"))
                            {
                                string range = $"[{sval.Substring(0, 3)}00 TO {sval.Substring(0, 3)}99]";
                                cf.SearchExpression = range;
                                cf.SearchShortcut = sname.Replace("_obecne", "");
                                cf.IsMainType = true;
                                cf.MainTypeValue = cf.Value;
                            }
                            else
                            {
                                cf.SearchExpression = sval;
                                cf.SearchShortcut = sname;
                                cf.IsMainType = false;
                                cf.MainTypeValue = (int)Math.Round(cf.Value/100m)*100;
                            }
                            AllTypes.Add(cf);
                        }


                    }
                }
            }


            public static List<ClassifField> AllTypes = null;

            public const int MinAcceptablePoints = 5;
            public const int MinAcceptablePointsSecond = 12;
            public const int MinAcceptablePointsThird = 24;

            public SClassification() { }

            public SClassification(Classification[] types)
            {
                LastUpdate = DateTime.Now;
                SetTypes(types);
            }

            private void SetTypes(Classification[] types)
            {
                if (types == null)
                    return;
                if (types.Count() == 0)
                    return;

                if (types?.Count() > 0)
                {
                    Class1 = types.OrderByDescending(o => o.ClassifProbability).First();
                    if (types.Count() > 1)
                        Class2 = types.OrderByDescending(o => o.ClassifProbability).Skip(1).First();
                    if (types?.Count() > 2)
                        Class3 = types.OrderByDescending(o => o.ClassifProbability).Skip(2).First();
                }

            }



            [ShowNiceDisplayName()]
            public enum ClassificationsTypes
            {


                [NiceDisplayName("Ostatní")]
                OSTATNI = 0,

                [NiceDisplayName("IT")]
                it_obecne = 10000,

                [NiceDisplayName("IT Hardware")]
                it_hw = 10001,
                [NiceDisplayName("IT Software (krabicový SW, licence, …)")]
                it_sw = 10002,
                //[NiceDisplayName("Informační systémy a servery")] // sloučeno se servery a cloudem
                //it_servery = 10003,
                [NiceDisplayName("Opravy, údržba a počítačové sítě")]
                it_site_opravy = 10004,
                [NiceDisplayName("IT Vývoj (na zakázku, včetně možné následné podpory)")]
                it_vyvoj = 10005,
                [NiceDisplayName("Konzultace a poradenství")]
                it_konzultace = 10006,
                //[NiceDisplayName("SW sluzby")] //sloučeno se servery a cloudem
                //it_sw_sluzby = 10007,
                [NiceDisplayName("Internetové služby, servery, cloud, certifikáty")]
                it_sluzby_servery = 10008,
                [NiceDisplayName("IT Bezpečnost")]
                it_bezpecnost = 10009,

                [NiceDisplayName("Stavebnictví")]
                stav_obecne = 10100,
                //[NiceDisplayName("Stavební konstrukce a materiály")]
                //stav_materialy = 10101,
                //[NiceDisplayName("Stavební práce")]
                //stav_prace = 10102,
                //[NiceDisplayName("Bytová výstavba")]
                //stav_byty = 10103,
                [NiceDisplayName("Konstrukční a stavební práce")]
                stav_konstr = 10104,
                //[NiceDisplayName("Stavební úpravy mostů a tunelů")]
                //stav_mosty = 10105,
                [NiceDisplayName("Stavební práce pro potrubní, telekomunikační a elektrické vedení")]
                stav_vedeni = 10106,
                [NiceDisplayName("Výstavba, zakládání a povrchové práce pro silnice")]
                stav_silnice = 10107,
                [NiceDisplayName("Stavební úpravy pro železnici")]
                stav_zeleznice = 10108,
                [NiceDisplayName("Výstavba vodních děl")]
                stav_voda = 10109,
                //[NiceDisplayName("Stavební montážní práce")]
                //stav_montaz = 10110,
                [NiceDisplayName("Práce při dokončování budov")]
                stav_dokonceni = 10111,
                //[NiceDisplayName("Opravy a údržba technických stavebních zařízení")]
                //stav_technik = 10112,
                //[NiceDisplayName("Instalační a montážní služby")]
                //stav_instal = 10113,
                [NiceDisplayName("Stavební služby (Architektonické, technické, inspekční, projektové, …)")]
                stav_sluzby = 10114,
                //[NiceDisplayName("Stavební služby (Architektonické, technické, inspekční, projektové, …)")]
                //stav_sluzba = 10115,

                [NiceDisplayName("Doprava a poštovní služby")]
                doprava_obecne = 10200,
                [NiceDisplayName("Osobní vozidla")]
                doprava_osobni = 10201,
                [NiceDisplayName("Nákladní nebo speciální vozidla (army, těžká technika, příslušenství)")]
                doprava_special = 10202,
                [NiceDisplayName("Hromadná autobusová a vlaková doprava")]
                doprava_lidi = 10203,
                //[NiceDisplayName("Nakladní vozy")]
                //doprava_nakladni = 10204,
                [NiceDisplayName("Vozidla silniční údržby a příslušenství")]
                doprava_udrzba = 10205,
                [NiceDisplayName("Sanitní a zdravotnická vozidla")]
                doprava_pohotovost = 10206,
                //[NiceDisplayName("Díly a příslušenství k motorovým vozidlům")]
                //doprava_dily = 10207,
                [NiceDisplayName("Železniční a tramvajové lokomotivy a vozidla")]
                doprava_koleje = 10208,
                //[NiceDisplayName("Silniční zařízení")]
                //doprava_silnice = 10209,
                [NiceDisplayName("Servis a oprava vozidel včetně náhradních dílů a příslušenství")]
                doprava_opravy = 10210,
                //[NiceDisplayName("Služby silniční dopravy")]
                //doprava_sluzby = 10211,
                [NiceDisplayName("Poštovní a kurýrní služby")]
                doprava_posta = 10212,
                [NiceDisplayName("Letecká přeprava")]
                doprava_letadla = 10213,

                [NiceDisplayName("Stroje a zařízení")]
                stroje_obecne = 10300,
                [NiceDisplayName("Elektricke stroje")]
                stroje_elektricke = 10301,
                [NiceDisplayName("Laboratorní přístroje a zařízení")]
                stroje_laborator = 10302,
                [NiceDisplayName("Průmyslové stroje")]
                stroje_prumysl = 10303,

                [NiceDisplayName("Telco")]
                telco_obecne = 10400,
                //[NiceDisplayName("TV")]
                //telco_tv = 10401,
                [NiceDisplayName("Sítě a přenos dat")]
                telco_site = 10402,
                [NiceDisplayName("Telekomunikační služby")]
                telco_sluzby = 10403,

                [NiceDisplayName("Zdravotnictví")]
                zdrav_obecne = 10500,
                [NiceDisplayName("Zdravotnické přístroje")]
                zdrav_pristroje = 10501,
                [NiceDisplayName("Leciva")]
                zdrav_leciva = 10502,
                [NiceDisplayName("Kosmetika")]
                zdrav_kosmetika = 10503,
                [NiceDisplayName("Opravy a údržba zdravotnických přístrojů")]
                zdrav_opravy = 10504,
                [NiceDisplayName("Zdravotnický materiál")]
                zdrav_material = 10505,
                [NiceDisplayName("Zdravotnický hygienický materiál")]
                zdrav_hygiena = 10506,

                [NiceDisplayName("Voda a potraviny")]
                jidlo_obecne = 10600,
                [NiceDisplayName("Potraviny")]
                jidlo_potrava = 10601,
                [NiceDisplayName("Pitná voda, nápoje, tabák atd.")]
                jidlo_voda = 10602,

                [NiceDisplayName("Bezpečnostní a ochranné vybavení a údržba")]
                bezpecnost_obecne = 10700,
                [NiceDisplayName("Kamerové systémy")]
                bezpecnost_kamery = 10701,
                [NiceDisplayName("Hasičské vybavení, požární ochrana")]
                bezpecnost_hasici = 10702,
                [NiceDisplayName("Zbraně")]
                bezpecnost_zbrane = 10703,
                [NiceDisplayName("Ostraha objektů")]
                bezpecnost_ostraha = 10704,

                //bezpecnost;bezpecnost-generic;Bezpečnost generická;35;506;5155;519
                //bezpecnost; bezpecnost; Bezpečnostní a ochranné vybavení a údržba;35;506;5155;519

                //


                [NiceDisplayName("Přírodní zdroje")]
                prirodnizdroj_obecne = 10800,
                [NiceDisplayName("Písky a jíly")]
                prirodnizdroj_pisky = 10801,
                [NiceDisplayName("Chemické výrobky")]
                prirodnizdroj_chemie = 10802,
                //[NiceDisplayName("Jiné přírodní zdroje")]
                //prirodnizdroj_vse = 10803,

                [NiceDisplayName("Energie")]
                energie_obecne = 10900,
                [NiceDisplayName("Paliva a oleje")]
                energie_paliva = 10901,
                [NiceDisplayName("Elektricka energie")]
                energie_elektrina = 10902,
                [NiceDisplayName("Jiná energie")]
                energie_jina = 10903,
                [NiceDisplayName("Veřejné služby pro energie")]
                energie_sluzby = 10904,
                [NiceDisplayName("Voda")]
                energie_voda = 10905,

                [NiceDisplayName("Zemědělství")] //mozna 
                agro_obecne = 11000,
                [NiceDisplayName("Lesnictví a těžba dřeva")]
                agro_les = 11001,
                //[NiceDisplayName("Těžba dřeva")]  //sloučeno s lesnictvím
                //agro_tezba = 11002,
                [NiceDisplayName("Zahradnické služby")]
                agro_zahrada = 11003,

                [NiceDisplayName("Kancelář")]
                kancelar_obecne = 11100,
                [NiceDisplayName("Tisk")]
                kancelar_tisk = 11101,
                [NiceDisplayName("Kancelářské potřeby (tiskárny, skenery, papírnické potřeby)")]
                kancelar_potreby = 11102,
                [NiceDisplayName("Nábytek")]
                kancelar_nabytek = 11103,
                [NiceDisplayName("Kancelářské a domácí spotřebiče (klimatizace, myčka, lednička, sporák, …)")]
                kancelar_spotrebice = 11104,
                [NiceDisplayName("Čisticí výrobky")]
                kancelar_cisteni = 11105,
                [NiceDisplayName("Nábor zaměstnanců")]
                kancelar_nabor = 11106,
                [NiceDisplayName("Mobily, smart zařízení")]
                kancelar_smart = 11107,
                [NiceDisplayName("Knihy, časopisy, literatura")]
                kancelar_literatura = 11108,


                [NiceDisplayName("Řemesla")]
                remeslo_obecne = 11200,
                [NiceDisplayName("Oděvy")]
                remeslo_odevy = 11201,
                [NiceDisplayName("Textilie")]
                remeslo_textil = 11202,
                [NiceDisplayName("Hudební nástroje")]
                remeslo_hudba = 11203,
                [NiceDisplayName("Sport a sportoviště")]
                remeslo_sport = 11204,

                [NiceDisplayName("Sociální služby")]
                social_obecne = 11300,
                // [NiceDisplayName("Vzdělávání a školení")] //přesunuto do 11701
                // social_vzdelavani = 11301,
                //[NiceDisplayName("Školení")] // sloučeno se vzděláváním
                //social_skoleni = 11302,
                [NiceDisplayName("Zdravotní péče")]
                social_zdravotni = 11303,
                [NiceDisplayName("Sociální péče")]
                social_pece = 11304,
                [NiceDisplayName("Rekreační, kulturní akce")]
                social_kultura = 11305,
                [NiceDisplayName("Knihovny, archivy, muzea a jiné")]
                social_knihovny = 11306,


                [NiceDisplayName("Finance")]
                finance_obecne = 11400,
                [NiceDisplayName("Pojišťovací služby")]
                finance_pojisteni = 11401,
                [NiceDisplayName("Účetní, revizní a peněžní služby")]
                finance_ucetni = 11402,
                [NiceDisplayName("Podnikatelské a manažerské poradenství a související služby")]
                finance_poradenstvi = 11403,
                //[NiceDisplayName("Dotace")]
                //finance_dotace = 11404,
                [NiceDisplayName("Bankovní služby a poplatky")]
                finance_bankovni = 11405,
                [NiceDisplayName("Spoření a repo operace")]
                finance_repo = 11406,
                [NiceDisplayName("Bankovní formality, ceníky, VOP")]
                finance_formality = 11407,

                [NiceDisplayName("Právní a realitní služby")]
                legal_obecne = 11500,
                [NiceDisplayName("Realitní služby (poradenství, konzultace)")]
                legal_reality = 11501,
                [NiceDisplayName("Právní služby")]
                legal_pravni = 11502,
                // tyhle dvě kategorie byly sloučeny do 11505
                // [NiceDisplayName("Nájemní smlouvy")]
                // legal_najem = 11503,
                // [NiceDisplayName("Pronájem pozemků")]
                // legal_pozemky = 11504,
                [NiceDisplayName("Nákup, prodej a pronájem nemovitosti (bytové i nebytové)")]
                legal_nemovitosti = 11505,


                [NiceDisplayName("Technické služby")]
                techsluzby_obecne = 11600,
                [NiceDisplayName("Odpady")]
                techsluzby_odpady = 11601,
                [NiceDisplayName("Čistící a hygienické služby")]
                techsluzby_cisteni = 11602,
                [NiceDisplayName("Úklidové služby")]
                techsluzby_uklid = 11603,


                [NiceDisplayName("Věda, výzkum a vzdělávání")]
                vyzkum_obecne = 11700,
                [NiceDisplayName("Školení a kurzy")]
                vyzkum_skoleni = 11701,

                [NiceDisplayName("Reklamní a marketingové služby")]
                marketing_obecne = 11800,
                //[NiceDisplayName("Reklamní a marketingové služby")]
                //[Disabled()]
                //marketing_reklama = 11801, //TODO remove

                [NiceDisplayName("Jiné služby")]
                jine_obecne = 11900,
                [NiceDisplayName("Pohostinství a ubytovací služby a maloobchodní služby")]
                jine_pohostinstvi = 11901,
                [NiceDisplayName("Služby závodních jídelen")]
                jine_jidelny = 11902,
                [NiceDisplayName("Administrativní služby, stravenky")]
                jine_admin = 11903,
                [NiceDisplayName("Zajišťování služeb pro veřejnost")]
                jine_verejnost = 11904,
                [NiceDisplayName("Průzkum veřejného mínění a statistiky")]
                jine_pruzkum = 11905,
                [NiceDisplayName("Opravy, údržba, záruční i pozáruční servis")]
                jine_opravy = 11906,
                [NiceDisplayName("Překladatelské a tlumočnické služby")]
                jine_preklady = 11907,

                [NiceDisplayName("Dary a dotace")]
                dary_obecne = 12000,
                [NiceDisplayName("Smlouvy o spolupráci")]
                dary_spoluprace = 12001,
                
                [NiceDisplayName("Zvířata (služební psi, koně)")]
                zvirata_obecne = 12100,
                
                
            }


            
            // následující půjde smazat až nebude existovat žádná smlouva ve verzi 1 
            // get hlidacsmluv/_search
            // {
            //     "query": {
            //         "match": {
            //             "classification.version": 1
            //         }
            //     }
            // }
            [Obsolete("Nahrazeno")]
            public Classification[] Types { get; set; } = null;


            [Nest.Date]
            public DateTime? LastUpdate { get; set; } = null;

            [Nest.Number]
            public int Version { get; set; } = 2;

            public Classification[] GetClassif()
            {
                List<Classification> types = new List<Classification>();
                if (Class1 != null)
                    types.Add(Class1);
                if (Class2 != null)
                    types.Add(Class2);
                if (Class3 != null)
                    types.Add(Class3);
                return types.ToArray();


            }

            public Classification Class1 { get; set; } = null;
            public Classification Class2 { get; set; } = null;
            public Classification Class3 { get; set; } = null;

            public bool HasClassification() => Class1 != null || Class2 != null || Class3 != null;

            public void ConvertToV2()
            {
                if (Version == 1 && Types != null)
                {
                    TypesToProperties(Types);
                    Types = null;
                }
            }

            public void TypesToProperties(Classification[] types)
            {
                Class1 = null;
                Class2 = null;
                Class3 = null;
                if (types?.Count() > 0)
                {
                    Class1 = types.OrderByDescending(o => o.ClassifProbability).First();
                    if (types.Count() > 1)
                        Class2 = types.OrderByDescending(o => o.ClassifProbability).Skip(1).First();
                    if (types?.Count() > 2)
                        Class3 = types.OrderByDescending(o => o.ClassifProbability).Skip(2).First();
                }
            }

            public override string ToString()
            {
                if (GetClassif() != null)
                {
                    return $"Types:{GetClassif().Select(m => m.ClassifType().ToString() + " (" + m.ClassifProbability.ToString("P2") + ")").Aggregate((f, s) => f + "; " + s)}"
                        + $" updated:{LastUpdate.ToString()}";
                }
                else
                {
                    return $"Types:snull updated:{LastUpdate.ToString()}";
                }
                //return base.ToString();
            }











        }




    }
}
