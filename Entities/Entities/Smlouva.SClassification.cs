using Devmasters.Enums;

using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                [Description("Other")]
                OSTATNI = 0,

                [NiceDisplayName("IT")]
                [Description("IT")]
                it_obecne = 10000,

                [NiceDisplayName("IT Hardware")]
                [Description("IT Hardware")]
                it_hw = 10001,

                [NiceDisplayName("IT Software (krabicový SW, licence, …)")]
                [Description("IT Software (boxed software, licenses, etc.)")]
                it_sw = 10002,

                //[NiceDisplayName("Informační systémy a servery")] // sloučeno se servery a cloudem
                //it_servery = 10003,

                [NiceDisplayName("Opravy, údržba a počítačové sítě")]
                [Description("Repairs, maintenance and computer networks")]
                it_site_opravy = 10004,

                [NiceDisplayName("IT Vývoj (na zakázku, včetně možné následné podpory)")]
                [Description("IT Development (custom, including possible follow-up support)")]
                it_vyvoj = 10005,

                [NiceDisplayName("Konzultace a poradenství")]
                [Description("Consulting and advisory services")]
                it_konzultace = 10006,

                //[NiceDisplayName("SW sluzby")] //sloučeno se servery a cloudem
                //it_sw_sluzby = 10007,

                [NiceDisplayName("Internetové služby, servery, cloud, certifikáty")]
                [Description("Internet services, servers, cloud, certificates")]
                it_sluzby_servery = 10008,

                [NiceDisplayName("IT Bezpečnost")]
                [Description("IT Security")]
                it_bezpecnost = 10009,

                [NiceDisplayName("Stavebnictví")]
                [Description("Construction")]
                stav_obecne = 10100,

                //[NiceDisplayName("Stavební konstrukce a materiály")]
                //stav_materialy = 10101,
                //[NiceDisplayName("Stavební práce")]
                //stav_prace = 10102,
                //[NiceDisplayName("Bytová výstavba")]
                //stav_byty = 10103,

                [NiceDisplayName("Konstrukční a stavební práce")]
                [Description("Structural and construction work")]
                stav_konstr = 10104,

                //[NiceDisplayName("Stavební úpravy mostů a tunelů")]
                //stav_mosty = 10105,

                [NiceDisplayName("Stavební práce pro potrubní, telekomunikační a elektrické vedení")]
                [Description("Construction work for pipeline, telecommunications and electrical lines")]
                stav_vedeni = 10106,

                [NiceDisplayName("Výstavba, zakládání a povrchové práce pro silnice")]
                [Description("Construction, foundation and surface work for roads")]
                stav_silnice = 10107,

                [NiceDisplayName("Stavební úpravy pro železnici")]
                [Description("Construction modifications for railways")]
                stav_zeleznice = 10108,

                [NiceDisplayName("Výstavba vodních děl")]
                [Description("Construction of water works")]
                stav_voda = 10109,

                //[NiceDisplayName("Stavební montážní práce")]
                //stav_montaz = 10110,

                [NiceDisplayName("Práce při dokončování budov")]
                [Description("Building completion work")]
                stav_dokonceni = 10111,

                //[NiceDisplayName("Opravy a údržba technických stavebních zařízení")]
                //stav_technik = 10112,
                //[NiceDisplayName("Instalační a montážní služby")]
                //stav_instal = 10113,

                [NiceDisplayName("Stavební služby (Architektonické, technické, inspekční, projektové, …)")]
                [Description("Construction services (Architectural, technical, inspection, project, etc.)")]
                stav_sluzby = 10114,

                //[NiceDisplayName("Stavební služby (Architektonické, technické, inspekční, projektové, …)")]
                //stav_sluzba = 10115,

                [NiceDisplayName("Doprava a poštovní služby")]
                [Description("Transport and postal services")]
                doprava_obecne = 10200,

                [NiceDisplayName("Osobní vozidla")]
                [Description("Personal vehicles")]
                doprava_osobni = 10201,

                [NiceDisplayName("Nákladní nebo speciální vozidla (army, těžká technika, příslušenství)")]
                [Description("Freight or special vehicles (army, heavy machinery, accessories)")]
                doprava_special = 10202,

                [NiceDisplayName("Hromadná autobusová a vlaková doprava")]
                [Description("Mass bus and rail transport")]
                doprava_lidi = 10203,

                //[NiceDisplayName("Nakladní vozy")]
                //doprava_nakladni = 10204,

                [NiceDisplayName("Vozidla silniční údržby a příslušenství")]
                [Description("Road maintenance vehicles and accessories")]
                doprava_udrzba = 10205,

                [NiceDisplayName("Sanitní a zdravotnická vozidla")]
                [Description("Ambulance and medical vehicles")]
                doprava_pohotovost = 10206,

                //[NiceDisplayName("Díly a příslušenství k motorovým vozidlům")]
                //doprava_dily = 10207,

                [NiceDisplayName("Železniční a tramvajové lokomotivy a vozidla")]
                [Description("Railway and tram locomotives and vehicles")]
                doprava_koleje = 10208,

                //[NiceDisplayName("Silniční zařízení")]
                //doprava_silnice = 10209,

                [NiceDisplayName("Servis a oprava vozidel včetně náhradních dílů a příslušenství")]
                [Description("Vehicle service and repair including spare parts and accessories")]
                doprava_opravy = 10210,

                //[NiceDisplayName("Služby silniční dopravy")]
                //doprava_sluzby = 10211,

                [NiceDisplayName("Poštovní a kurýrní služby")]
                [Description("Postal and courier services")]
                doprava_posta = 10212,

                [NiceDisplayName("Letecká přeprava")]
                [Description("Air transport")]
                doprava_letadla = 10213,

                [NiceDisplayName("Stroje a zařízení")]
                [Description("Machines and equipment")]
                stroje_obecne = 10300,

                [NiceDisplayName("Elektricke stroje")]
                [Description("Electric machines")]
                stroje_elektricke = 10301,

                [NiceDisplayName("Laboratorní přístroje a zařízení")]
                [Description("Laboratory instruments and equipment")]
                stroje_laborator = 10302,

                [NiceDisplayName("Průmyslové stroje")]
                [Description("Industrial machines")]
                stroje_prumysl = 10303,

                [NiceDisplayName("Telco")]
                [Description("Telco")]
                telco_obecne = 10400,

                //[NiceDisplayName("TV")]
                //telco_tv = 10401,

                [NiceDisplayName("Sítě a přenos dat")]
                [Description("Networks and data transmission")]
                telco_site = 10402,

                [NiceDisplayName("Telekomunikační služby")]
                [Description("Telecommunications services")]
                telco_sluzby = 10403,

                [NiceDisplayName("Zdravotnictví")]
                [Description("Healthcare")]
                zdrav_obecne = 10500,

                [NiceDisplayName("Zdravotnické přístroje")]
                [Description("Medical instruments")]
                zdrav_pristroje = 10501,

                [NiceDisplayName("Leciva")]
                [Description("Medicines")]
                zdrav_leciva = 10502,

                [NiceDisplayName("Kosmetika")]
                [Description("Cosmetics")]
                zdrav_kosmetika = 10503,

                [NiceDisplayName("Opravy a údržba zdravotnických přístrojů")]
                [Description("Repair and maintenance of medical instruments")]
                zdrav_opravy = 10504,

                [NiceDisplayName("Zdravotnický materiál")]
                [Description("Medical materials")]
                zdrav_material = 10505,

                [NiceDisplayName("Zdravotnický hygienický materiál")]
                [Description("Medical hygiene materials")]
                zdrav_hygiena = 10506,

                [NiceDisplayName("Voda a potraviny")]
                [Description("Water and food")]
                jidlo_obecne = 10600,

                [NiceDisplayName("Potraviny")]
                [Description("Food")]
                jidlo_potrava = 10601,

                [NiceDisplayName("Pitná voda, nápoje, tabák atd.")]
                [Description("Drinking water, beverages, tobacco, etc.")]
                jidlo_voda = 10602,

                [NiceDisplayName("Bezpečnostní a ochranné vybavení a údržba")]
                [Description("Security and protective equipment and maintenance")]
                bezpecnost_obecne = 10700,

                [NiceDisplayName("Kamerové systémy")]
                [Description("Camera systems")]
                bezpecnost_kamery = 10701,

                [NiceDisplayName("Hasičské vybavení, požární ochrana")]
                [Description("Fire equipment, fire protection")]
                bezpecnost_hasici = 10702,

                [NiceDisplayName("Zbraně")]
                [Description("Weapons")]
                bezpecnost_zbrane = 10703,

                [NiceDisplayName("Ostraha objektů")]
                [Description("Object security")]
                bezpecnost_ostraha = 10704,

                //bezpecnost;bezpecnost-generic;Bezpečnost generická;35;506;5155;519
                //bezpecnost; bezpecnost; Bezpečnostní a ochranné vybavení a údržba;35;506;5155;519

                //

                [NiceDisplayName("Přírodní zdroje")]
                [Description("Natural resources")]
                prirodnizdroj_obecne = 10800,

                [NiceDisplayName("Písky a jíly")]
                [Description("Sands and clays")]
                prirodnizdroj_pisky = 10801,

                [NiceDisplayName("Chemické výrobky")]
                [Description("Chemical products")]
                prirodnizdroj_chemie = 10802,

                //[NiceDisplayName("Jiné přírodní zdroje")]
                //prirodnizdroj_vse = 10803,

                [NiceDisplayName("Energie")]
                [Description("Energy")]
                energie_obecne = 10900,

                [NiceDisplayName("Paliva a oleje")]
                [Description("Fuels and oils")]
                energie_paliva = 10901,

                [NiceDisplayName("Elektricka energie")]
                [Description("Electric energy")]
                energie_elektrina = 10902,

                [NiceDisplayName("Jiná energie")]
                [Description("Other energy")]
                energie_jina = 10903,

                [NiceDisplayName("Veřejné služby pro energie")]
                [Description("Public energy services")]
                energie_sluzby = 10904,

                [NiceDisplayName("Voda")]
                [Description("Water")]
                energie_voda = 10905,

                [NiceDisplayName("Zemědělství")] //mozna 
                [Description("Agriculture")]
                agro_obecne = 11000,

                [NiceDisplayName("Lesnictví a těžba dřeva")]
                [Description("Forestry and logging")]
                agro_les = 11001,

                //[NiceDisplayName("Těžba dřeva")]  //sloučeno s lesnictvím
                //agro_tezba = 11002,

                [NiceDisplayName("Zahradnické služby")]
                [Description("Gardening services")]
                agro_zahrada = 11003,

                [NiceDisplayName("Kancelář")]
                [Description("Office")]
                kancelar_obecne = 11100,

                [NiceDisplayName("Tisk")]
                [Description("Printing")]
                kancelar_tisk = 11101,

                [NiceDisplayName("Kancelářské potřeby (tiskárny, skenery, papírnické potřeby)")]
                [Description("Office supplies (printers, scanners, stationery)")]
                kancelar_potreby = 11102,

                [NiceDisplayName("Nábytek")]
                [Description("Furniture")]
                kancelar_nabytek = 11103,

                [NiceDisplayName("Kancelářské a domácí spotřebiče (klimatizace, myčka, lednička, sporák, …)")]
                [Description("Office and household appliances (air conditioning, dishwasher, refrigerator, stove, etc.)")]
                kancelar_spotrebice = 11104,

                [NiceDisplayName("Čisticí výrobky")]
                [Description("Cleaning products")]
                kancelar_cisteni = 11105,

                [NiceDisplayName("Nábor zaměstnanců")]
                [Description("Employee recruitment")]
                kancelar_nabor = 11106,

                [NiceDisplayName("Mobily, smart zařízení")]
                [Description("Mobile phones, smart devices")]
                kancelar_smart = 11107,

                [NiceDisplayName("Knihy, časopisy, literatura")]
                [Description("Books, magazines, literature")]
                kancelar_literatura = 11108,

                [NiceDisplayName("Řemesla")]
                [Description("Crafts")]
                remeslo_obecne = 11200,

                [NiceDisplayName("Oděvy")]
                [Description("Clothing")]
                remeslo_odevy = 11201,

                [NiceDisplayName("Textilie")]
                [Description("Textiles")]
                remeslo_textil = 11202,

                [NiceDisplayName("Hudební nástroje")]
                [Description("Musical instruments")]
                remeslo_hudba = 11203,

                [NiceDisplayName("Sport a sportoviště")]
                [Description("Sports and sports facilities")]
                remeslo_sport = 11204,

                [NiceDisplayName("Sociální služby")]
                [Description("Social services")]
                social_obecne = 11300,

                // [NiceDisplayName("Vzdělávání a školení")] //přesunuto do 11701
                // social_vzdelavani = 11301,
                //[NiceDisplayName("Školení")] // sloučeno se vzděláváním
                //social_skoleni = 11302,

                [NiceDisplayName("Zdravotní péče")]
                [Description("Healthcare")]
                social_zdravotni = 11303,

                [NiceDisplayName("Sociální péče")]
                [Description("Social care")]
                social_pece = 11304,

                [NiceDisplayName("Rekreační, kulturní akce")]
                [Description("Recreational, cultural events")]
                social_kultura = 11305,

                [NiceDisplayName("Knihovny, archivy, muzea a jiné")]
                [Description("Libraries, archives, museums and others")]
                social_knihovny = 11306,

                [NiceDisplayName("Finance")]
                [Description("Finance")]
                finance_obecne = 11400,

                [NiceDisplayName("Pojišťovací služby")]
                [Description("Insurance services")]
                finance_pojisteni = 11401,

                [NiceDisplayName("Účetní, revizní a peněžní služby")]
                [Description("Accounting, audit and financial services")]
                finance_ucetni = 11402,

                [NiceDisplayName("Podnikatelské a manažerské poradenství a související služby")]
                [Description("Business and management consulting and related services")]
                finance_poradenstvi = 11403,

                //[NiceDisplayName("Dotace")]
                //finance_dotace = 11404,

                [NiceDisplayName("Bankovní služby a poplatky")]
                [Description("Banking services and fees")]
                finance_bankovni = 11405,

                [NiceDisplayName("Spoření a repo operace")]
                [Description("Savings and repo operations")]
                finance_repo = 11406,

                [NiceDisplayName("Bankovní formality, ceníky, VOP")]
                [Description("Banking formalities, price lists, terms and conditions")]
                finance_formality = 11407,

                [NiceDisplayName("Právní a realitní služby")]
                [Description("Legal and real estate services")]
                legal_obecne = 11500,

                [NiceDisplayName("Realitní služby (poradenství, konzultace)")]
                [Description("Real estate services (advisory, consultation)")]
                legal_reality = 11501,

                [NiceDisplayName("Právní služby")]
                [Description("Legal services")]
                legal_pravni = 11502,

                // tyhle dvě kategorie byly sloučeny do 11505
                // [NiceDisplayName("Nájemní smlouvy")]
                // legal_najem = 11503,
                // [NiceDisplayName("Pronájem pozemků")]
                // legal_pozemky = 11504,

                [NiceDisplayName("Nákup, prodej a pronájem nemovitosti (bytové i nebytové)")]
                [Description("Purchase, sale and rental of real estate (residential and commercial)")]
                legal_nemovitosti = 11505,

                [NiceDisplayName("Technické služby")]
                [Description("Technical services")]
                techsluzby_obecne = 11600,

                [NiceDisplayName("Odpady")]
                [Description("Waste")]
                techsluzby_odpady = 11601,

                [NiceDisplayName("Čistící a hygienické služby")]
                [Description("Cleaning and hygiene services")]
                techsluzby_cisteni = 11602,

                [NiceDisplayName("Úklidové služby")]
                [Description("Cleaning services")]
                techsluzby_uklid = 11603,

                [NiceDisplayName("Věda, výzkum a vzdělávání")]
                [Description("Science, research and education")]
                vyzkum_obecne = 11700,

                [NiceDisplayName("Školení a kurzy")]
                [Description("Training and courses")]
                vyzkum_skoleni = 11701,

                [NiceDisplayName("Reklamní a marketingové služby")]
                [Description("Advertising and marketing services")]
                marketing_obecne = 11800,

                //[NiceDisplayName("Reklamní a marketingové služby")]
                //[Disabled()]
                //marketing_reklama = 11801, //TODO remove

                [NiceDisplayName("Jiné služby")]
                [Description("Other services")]
                jine_obecne = 11900,

                [NiceDisplayName("Pohostinství a ubytovací služby a maloobchodní služby")]
                [Description("Hospitality and accommodation services and retail services")]
                jine_pohostinstvi = 11901,

                [NiceDisplayName("Služby závodních jídelen")]
                [Description("Company canteen services")]
                jine_jidelny = 11902,

                [NiceDisplayName("Administrativní služby, stravenky")]
                [Description("Administrative services, meal vouchers")]
                jine_admin = 11903,

                [NiceDisplayName("Zajišťování služeb pro veřejnost")]
                [Description("Providing services to the public")]
                jine_verejnost = 11904,

                [NiceDisplayName("Průzkum veřejného mínění a statistiky")]
                [Description("Public opinion research and statistics")]
                jine_pruzkum = 11905,

                [NiceDisplayName("Opravy, údržba, záruční i pozáruční servis")]
                [Description("Repairs, maintenance, warranty and post-warranty service")]
                jine_opravy = 11906,

                [NiceDisplayName("Překladatelské a tlumočnické služby")]
                [Description("Translation and interpretation services")]
                jine_preklady = 11907,

                [NiceDisplayName("Dary a dotace")]
                [Description("Gifts and grants")]
                dary_obecne = 12000,

                [NiceDisplayName("Smlouvy o spolupráci")]
                [Description("Cooperation agreements")]
                dary_spoluprace = 12001,

                [NiceDisplayName("Zvířata (služební psi, koně)")]
                [Description("Animals (service dogs, horses)")]
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
                    string types = string.Join("; ",
                        GetClassif().Select(m =>
                            m.ClassifType().ToString() + " (" + m.ClassifProbability.ToString("P2") + ")"));
                    return $"Types:{types} updated:{LastUpdate.ToString()}";
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
