using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Devmasters.Enums;
using HlidacStatu.Util;
using Nest;
using static HlidacStatu.Entities.Firma;

namespace HlidacStatu.Entities;

public partial class Subsidy
{
    public class Hint
    {
        public Category? Category1 { get; set; }
        public Category? Category2 { get; set; }
        public Category? Category3 { get; set; }

        /// <summary>
        ///     Info zda-li se jedná o pravděpodobnou duplicitní dotace
        /// </summary>
        [Keyword]
        public bool IsOriginal { get; set; } = true;

        [Keyword]
        public string? OriginalSubsidyId { get; set; }

        public List<string> Duplicates { get; set; } = new List<string>();

        public List<string> HiddenDuplicates { get; set; } = new List<string>();

        [Keyword]
        public bool HasDuplicates => Duplicates.Any();
        [Keyword]
        public bool HasHiddenDuplicates => HiddenDuplicates.Any();


        /// <summary>
        ///     Legislativa - info, jestli jde o státní/evropskou dotaci, krajskou, obecní a nebo investiční pobídka
        /// </summary>
        [Keyword]
        public Type SubsidyType { get; set; }

        [Date]
        public DateTime? DuplicateCalculated { get; set; }


        //recipient hints
        public int RecipientTypSubjektu { get; set; } = -1; //Firma.TypSubjektuEnum

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Nest.Ignore]
        public Firma.TypSubjektuEnum RecipientTypSubjektuEnum { get => (Firma.TypSubjektuEnum)this.RecipientTypSubjektu; }

        public int RecipientStatus { get; set; } = -1; //Firma.Status

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Nest.Ignore]
        public string RecipientStatusFull { get => Firma.StatusFull(this.RecipientStatus, false); }

        public int RecipientPolitickyAngazovanySubjekt { get; set; } //HintSmlouva.PolitickaAngazovanostTyp

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        [Nest.Ignore]
        public HintSmlouva.PolitickaAngazovanostTyp RecipientPolitickyAngazovanySubjektEnum { get => (HintSmlouva.PolitickaAngazovanostTyp)this.RecipientPolitickyAngazovanySubjekt; }

        public int RecipientPocetLetOdZalozeni { get; set; } = 9999;

        //recipient hints

        public enum Type
        {
            [NiceDisplayName("neznámý typ")]
            Unknown = 0,
            [NiceDisplayName("Státní, nebo evropská dotace")]
            Evropska = 1,
            [NiceDisplayName("Krajská dotace")]
            Krajska = 2,
            [NiceDisplayName("Obecní dotace")]
            Obecni = 3,
            [NiceDisplayName("Investiční pobídka")]
            InvesticniPobidka = 4
        }

        public static string TypeDescription(int? typ, int pad, bool jednotne = true)
        {
            if (typ.HasValue)
                return TypeDescription((Type)typ, pad, jednotne);
            else
                return "";
        }
        public static string TypeDescription(Type typ, int pad, bool jednotne = true)
        {
            if (jednotne)
            {
                switch (typ)
                {
                    case Type.Unknown:
                        if (pad == 4)
                            return "neznámé dotaci";
                        else
                            return "neznámá dotace";
                    case Type.Evropska:
                        if (pad == 4)
                            return "evropské dotaci";
                        else
                            return "evropská dotace";
                    case Type.Krajska:
                        if (pad == 4)
                            return "krajskou dotaci";
                        else
                            return "krajská dotace";
                    case Type.Obecni:
                        if (pad == 4)
                            return "obecní dotaci";
                        else
                            return "obecní dotace";
                    case Type.InvesticniPobidka:
                        if (pad == 4)
                            return "investiční pobídku";
                        else
                            return "investiční pobídka";
                    default:
                        return "";
                }
            }
            else
            {

                switch (typ)
                {
                    case Type.Unknown:
                        if (pad == 4)
                            return "neznámých dotací";
                        else
                            return "neznáme dotace";
                    case Type.Evropska:
                        if (pad == 4)
                            return "evropských dotací";
                        else
                            return "evropské dotace";
                    case Type.Krajska:
                        if (pad == 4)
                            return "krajských dotací";
                        else
                            return "krajské dotace";
                    case Type.Obecni:
                        if (pad == 4)
                            return "obecních dotací";
                        else
                            return "obecní dotace";
                    case Type.InvesticniPobidka:
                        if (pad == 4)
                            return "investičních pobídek";
                        else
                            return "investiční pobídky";
                    default:
                        return "";

                }
            }
        }

        public void SetDuplicate(Subsidy subsidy, List<string> duplicates, List<string> hiddenDuplicates, string originalSubsidyId)
        {
            if(string.IsNullOrEmpty(originalSubsidyId))
                return;
                
            if (originalSubsidyId == subsidy.Id)
            {
                //set as original
                IsOriginal = true;
                OriginalSubsidyId = null;
                DuplicateCalculated = DateTime.Now;
                Duplicates = duplicates.Where(d => d != subsidy.Id).ToList();
                HiddenDuplicates = hiddenDuplicates.Where(d => d != subsidy.Id).ToList();;

            }
            else
            {
                //set as duplicate
                IsOriginal = false;
                OriginalSubsidyId = originalSubsidyId; 
                DuplicateCalculated = DateTime.Now;
                Duplicates = duplicates.Where(d => d != subsidy.Id).ToList();
                HiddenDuplicates = hiddenDuplicates.Where(d => d != subsidy.Id).ToList();
            }
        }

        #region Categories
        
        [ShowNiceDisplayName]
        public enum CalculatedCategories
        {
            [NiceDisplayName("Ostatní")]
            Ostatni = 0,

            [NiceDisplayName("Digitální transformace a IT ")]
            DigiIt = 1000,

            [NiceDisplayName("Doprava a infrastruktura")]
            Doprava = 2000,

            [NiceDisplayName("Energetika a obnovitelné zdroje")]
            Energetika = 3000,

            [NiceDisplayName("Humanitární pomoc a rozvojová spolupráce")]
            HumanitarniPomoc = 4000,

            [NiceDisplayName("Inovace, výzkum a vývoj")]
            InovaceVyzkum = 5000,

            [NiceDisplayName("Kultura, kreativní průmysly a média")]
            KulturaMedia = 6000,

            [NiceDisplayName("Malé a střední podniky (MSP)")]
            MaleStredniPodniky = 7000,

            [NiceDisplayName("Obrana a bezpečnost")]
            ObranaBezpecnost = 8000,

            [NiceDisplayName("Památková péče a cestovní ruch")]
            CestovniRuchPamatky = 9000,

            [NiceDisplayName("Podpora demokracie a právního státu")]
            PravniStat = 10000,

            [NiceDisplayName("Podpora podnikání a investic")]
            PodporaPodnikani = 11000,

            [NiceDisplayName("Podpora zaměstnanosti a trhu práce")]
            ZamestnanostTrhPrace = 12000,

            [NiceDisplayName("Prevence kriminality a bezpečnost")]
            PrevenceKriminality = 13000,

            [NiceDisplayName("Regionální rozvoj, podpora, investice a soudržnost")]
            RegionalniRozvoj = 14000,

            [NiceDisplayName("Sociální služby, začleňování a zaměstnanost")]
            SocialniSluzby = 15000,

            [NiceDisplayName("Sport a volnočasové aktivity")]
            Sport = 16000,

            [NiceDisplayName("Vzdělávání,školství a mládež")]
            VzdelavaniSkolstvi = 17000,

            [NiceDisplayName("Zdraví a zdravotnické projekty")]
            Zdravotnictvi = 18000,

            [NiceDisplayName("Zemědělství, lesnictví a rozvoj venkova")]
            Zemedelstvi = 19000,

            [NiceDisplayName("Životní prostředí, klima a ekologické projekty")]
            ZivotniProstredi = 20000,

            [NiceDisplayName("Bilaterální zahraniční rozvojové spolupráce (ZRS), rozvoj občanské společnosti")]
            PodporaDoZahranici = 21000,

            [NiceDisplayName("Fond hejtmana")]
            FondHejtmana = 51000,

            [NiceDisplayName("Fond primátora")]
            FondPrimatora = 52000
        }

        

        public static readonly Dictionary<CalculatedCategories, string[]> CategoryNameDictionary = new()
        {
            { CalculatedCategories.Ostatni, new string[] { } },
            { CalculatedCategories.DigiIt, new[] { 
                "86594346",
           } },
            {
                CalculatedCategories.Doprava, new[]
                {
                    "70856508", "66003008",
                    "silniční hospodářství", "doprava"
                }
            },
            { CalculatedCategories.Energetika, new string[] { } },
            { CalculatedCategories.HumanitarniPomoc, new string[] { } },
            { CalculatedCategories.InovaceVyzkum, new[] { "48549037" } },
            {
                CalculatedCategories.KulturaMedia, new[]
                {
                    "01454455", "45806985", "00023671",
                    "kultura"
                }
            },
            { CalculatedCategories.MaleStredniPodniky, new string[] { } },
            { CalculatedCategories.ObranaBezpecnost, new[] { "60162694", "krizové oddělení" } },
            { CalculatedCategories.CestovniRuchPamatky, new[] { "cestovní ruch" } },
            { CalculatedCategories.PravniStat, new string[] { } },
            { CalculatedCategories.PodporaPodnikani, new string[] { } },
            {
                CalculatedCategories.ZamestnanostTrhPrace, new[]
                {
                    //urady prace
                    "00209554", "00567167", "72496991", "00564222", "00521108", "00522848", "00663875", "00508691",
                    "00663204", "00662348", "00559229", "00575771", "00555258", "00477273", "00515132", "00518514",
                    "00555720", "00653861", "00556556", "00521035", "00653977", "00214167", "00554715", "64095487",
                    "00655678", "00558231", "00666602", "00521086", "00560791", "00523984", "00510441", "00567639",
                    "00554910", "00555096", "00556513", "00510416", "00554898", "00653489", "00562700", "00576166",
                    "00560871", "00655040", "00665312", "00477087", "00519073", "00475904", "00575470", "00508675",
                    "00557561", "00508721", "00519731", "00655066", "00519723", "00562599", "00512451", "00519677",
                    "00554774", "00530123", "00554758", "00476234", "00653721", "00562394", "00557552", "00206172",
                    "00560014", "00566306", "Operační program Zaměstnanost"
                }
            },
            { CalculatedCategories.PrevenceKriminality, new string[] { } },
            { CalculatedCategories.RegionalniRozvoj, new[] { "regionální politika", "rozvoj" } },
            { CalculatedCategories.SocialniSluzby, new[] { "sociální věci" } },
            {
                CalculatedCategories.Sport, new[]
                {
                    "07460121",
                    "sport"
                }
            },
            {
                CalculatedCategories.VzdelavaniSkolstvi, new[]
                {
                    "školství"
                }
            },
            { CalculatedCategories.PodporaDoZahranici, new string[] { } },
            { CalculatedCategories.Zdravotnictvi, new[] { "zdravotnictví" } },
            { CalculatedCategories.Zemedelstvi, new[] { "48133981" } },
            { CalculatedCategories.ZivotniProstredi, new[] { "životní prostředí" } },
            { CalculatedCategories.FondHejtmana, new[] { "kancelář hejtmana" } },
            { CalculatedCategories.FondPrimatora, new string[] { } }
        };


        public static readonly Dictionary<CalculatedCategories, string[]> ProgramsDictionary = new()
        {
            { CalculatedCategories.Ostatni, new string[] { } },
            {
                CalculatedCategories.DigiIt, new[]
                {
                    "Podpora reprodukce majetku soc. služeb v oblasti ICT", "013D31100",
                    "Pořízení a obnova ICT center sociálních služeb", "013V33100",
                    "Pořízení obnova a provozování ICT ústavů sociální péče MPSV", "113V33100", "86594346",
                    "Pořízení obnova a provozování ICT systému řízení MZe", "129V01100",
                    "Pořízení obnova a provozování ICT systému řízení Mze", "329-229011",
                    "Rozvoj a obnova mat.tech.základny systému řízení Ministerstva zemědělství-od r.2007", "129D01000",
                    "Digitální transformace podniků", "Z220303000000", "Program Czech Rise-Up", "Z220308000000",
                    "Digitální ekonomika a společnost inovativní start-upy a nové technologie", "Z220311000000",
                    "Vytvoření evropských a národních center digitálních inovací (EDIHs) 1.5.01", "Z220318000000",
                    "Pořízení obnova a provozování ICT systému řízení MPO", "122V01100", "1.5 AI TEF - Manufacturing",
                    "Z220328000000", "Pořízení obnova a provozování ICT systému řízení MPO", "222011",
                    "Program Czech Rise Up - Chytrá opatření proti COVID-19 agregace", "Z221701000000",
                    "1.1. Digitální služby občanům a firmám", "Z220314000000",
                    "CEDMO - Central European Digital Media Observatory", "Z220312000000",
                    "Digitální ekonomika a společnost inovativní start-upy a nové technologie", "Z222901000000",
                    "Digitální vysokorychlostní sítě", "Z220302000000",
                    "Umělá inteligence pro bezpečnější společnost agregace", "Z223001000000",
                    "IT vybavení pro zaměstnance stavebních úřadů agregace", "Z171601000000",
                    "Pořízení obnova a provoz ICT", "117V01100",
                    "Národní plán obnovy - digitalizace agregace", "Z333201000000",
                    "9A Společný podnik pro klíčové digitální technologie (KDT JU) agregace", "Z335401000000",
                    "Rozvoj informační technologie ve vysokoškolském vzdělávání", "333321",
                    "Zvýšení technologické úrovně vysokoškolských knihoven", "333324",
                    "Pořízení a obnova kancelářské audiovizuální a jiné techniky", "333223",
                    "Zavádění a rozšiřování ICT do činnosti VVŠ", "333329",
                    "Podpora rozvoje obnovy a provozování ICT UK v Praze", "233311",
                    "Rozvoj informační technologie ve středoškolském vzdělávání", "333221",
                    "Rozvoj informační technologie v základním vzdělávání", "333121",
                    "GG 4.1. Smart Administration", "CZ.1.04/4.1.01", "Internetizace obcí", "314047",
                    "Pořízení obnova a provozování ICT v oblasti prevence kriminality", "314-214051",
                    "Pořízení obnova a provozování ICT organizací zabezpečující služby MV", "214041",
                    "Pořízení obnova a provozování ICT v oblasti prevence kriminality", "214051",
                    "Reprodukce investičního majetku materiálně-technické základny výroby tiskopisů a dokladů",
                    "314033", "Pořízení obnova a provozování ICT školství vzdělávání a tělovýchovy", "114V02100",
                    "Pořízení obnova a provozování ICT školství vzdělávání a tělovýchovy", "214V02100",
                    "Jednotné aplikační a komunikační prostředí - krajská úroveň", "314152",
                    "Jednotné aplikační a komunikační prostředí - okresní úroveň", "314151",
                    "Národní plán obnovy - Digitální služby občanům a firmám", "Z143501000000",
                    "Národní plán obnovy - Digitální systémy státní správy", "Z143502000000",
                    "Pořízení obnova a provozování ICT regionálního zdravotnictví", "235211",
                    "Pořízení obnova a provozování ICT regionálního zdravotnictví", "235D21100",
                    "Pořízení obnova a provozování ICT systému řízení fakultních nemocnic", "235111",
                    "Pořízení obnova a provozování ICT ZZS", "135D08100",
                    "Pořízení obnova a provozování ICT léčebných ústavů", "235131",
                    "Pořízení obnova a provozování ICT systému řízení fakultních nemocnic", "235V11100",
                    "Pořízení obnova a provozování ICT systému řízení MZ", "235011",
                    "Pořízení obnova a provozování ICT nemocnic ve státním vlastnictví", "235121",
                    "Pořízení obnova a provozování ICT léčebných ústavů", "235V13100",
                    "Pořízení obnova a provozování ICT regionálního zdravotnictví", "335-235211",
                    "Pořízení obnova a provozování ICT", "136V01100",
                                   "1.1. Digitální služby občanům a firmám",
                    "Digitální ekonomika a společnost, inovativní start-upy a nové technologie",
                    "Digitální transformace podniků",
                    "Digitální vysokorychlostní sítě",
                    "Národní plán obnovy - digitalizace agregace",
                    "Národní plán obnovy - Digitální služby občanům a firmám",
                    "Národní plán obnovy - Digitální systémy státní správy",
                    "Program digitální Evropa - projekt NCC-CZ (Národní koordinační centrum)",
                    "Program digitální Evropa - projekt TEST-CERT CZ",
                    "Středočeský Fond podpory cestovního ruchu - Digitalizace či modernizace TIC",
                    "Vytvoření evropských a národních center digitálních inovací (EDIHs) 1.5.01",
                    "Evropský metrologický program pro inovace a výzkum agregace",
                    "OP Podnikání a inovace",
                    "OP Výzkum a vývoj pro inovace",
                    "Informační a komunikační technologie",
                    "Rozvoj informační technologie",
                    "Umělá inteligence pro bezpečnější společnost agregace"
                }
            },
            {
                CalculatedCategories.Doprava, new[]
                {
                    "Podpora obnovy místních komunikací", "117D8210A", "Doprava", "Podpora obnovy místních komunikací",
                    "117D8220A", "Doprava", "Podpora obnovy staveb a zařízení dopravní infrastruktury", "117D8210C",
                    "Doprava", "OP Infrastruktura - doprava", "CZ.04.1.22", "Doprava",
                    "Podpora obnovy místních komunikací", "117D8230A", "Doprava", "FS: sektor Doprava",
                    "2004/CZ/16/C/PT", "Doprava",
                    "Mimořádné situace agregace", "Z270401000000", "Reprodukce strojů a zařízení pro údržbu silnic",
                    "327021", "Operační program Doprava", "4", "Podpora obnovy autobusů veřejné linkové dopravy",
                    "327-227624", "Odstranění škod způsobených povodněmi 1997", "327181",
                    "Podpora obnovy vozidel veřejné linkové dopravy", "127D62300",
                    "Podpora obnovy autobusů veřejné linkové dopravy", "227624",
                    "Rekonstrukce modernizace a opravy silnic I.třídy", "327112",
                    "Rekonstrukce modernizace a opravy silnic II.třídy", "327132",
                    "Rekonstrukce modernizace a opravy dálnice D1 Praha -Brno -Lipník n.Bečvou", "327298",
                    "Podpora obnovy vozidel povrchové městské hromadné dopravy", "227622",
                    "Rekonstrukce a modernizace celostátních tratí", "327321",
                    "Podpora obnovy vozidel povrchové městské hromadné dopravy", "327-227622",
                    "Obnova železniční dopravní infrastruktury po povodni 2002", "227823", "OP Doprava", "CZ.1.01",
                    "Úspory energie v resortu dopravy", "327-227032", "Výstavba obchvatů sídel a nových silnic I.třídy",
                    "327111", "Obnova staveb vodní dopravy po povodni 2002", "227824",
                    "Rekonstrukce modernizace a opravy mostů na silnicích II.třídy", "327133",
                    "Podpora obnovy vozidel městské hromadné dopravy", "127D62200",
                    "Podpora pořízení a obnovy vozidel autobusové dopravní obslužnosti", "327630",
                    "Podpora obnovy vozidel veřejné linkové dopravy", "327-127623",
                    "Rekonstrukce modernizace a opravy mostů na silnicích III.třídy", "327143",
                    "Obnova místních komunikací po povodni v červnu 2013", "127D21200",
                    "Pomoc státu při odstra. škod vznik. povod.2002 na maj.subj.provoz.veřej.přístavy a vnitroz.vod.dop.",
                    "227815", "Rekonstrukce modernizace a opravy silnic III.třídy", "327142",
                    "Pořízení a obnova budov staveb a vybavení SÚS", "327022",
                    "Obnova dálnic silnic pro motorová vozidla a silnic I.třídy po povodni 2002", "227822",
                    "Podpora obnovy historických železničních kolejových vozidel v období 2017 – 2020", "127D66200",
                    "Rekonstrukce modernizace a opravy mostů na silnicích I.třídy", "327113",
                    "Podpora obnovy vozidel městské hromadné dopravy", "327-127622", "OP Infrastruktura - doprava",
                    "CZ.04.1.22", "Podpora obnovy historických železničních kolejových vozidel", "127D64200",
                    "Odstranění povodňových škod 1997", "327381", "Odstranění škod způsobených povodněmi 1998",
                    "327182", "Pořízení a obnova autobusů a mikrobusů", "327623",
                    "Výstavba obchvatů sídel a nových silnic II.třídy", "327131",
                    "Podpora obnovy historických železničních kolejových vozidel v období 2021 – 2023", "127D67200",
                    "Ostatní výdaje spojené s drážní dopravou", "Z270305000000",
                    "Úhrada ztráty ze závazku veřejné služby ve veřejné drážní osobní dopravě", "Z270302000000",
                    "Podpora výstavby a obnovy silnic II. a III.třídy – alokace podle usnesení PSP k rozpočtu na rok 2003",
                    "227112", "Rozvoj a obnova mat.tech.základny Ředitelství silnic a dálnic", "22701B",
                    "Podpora pořízení a obnovy železničních kolejových vozidel v regionální osobní dopravě",
                    "327-227612", "Rekonstrukce modernizace a opravy dálnice D5 Praha -Rozvadov -SRN", "327291",
                    "Rekonstrukce modernizace a opravy dálnice D2 Brno -SR", "327299",
                    "Reprodukce investičního majetku Ředitelství silnic a dálnic", "327012",
                    "Zlepšení stavu mezinárodních silnic - projekt A", "327114",
                    "Pořízení obnova a provozování ICT systému řízení MD - PO", "127V02100",
                    "R 35 Hrádek nad Nisou - H.Králové - Mohelnice - Olomouc", "327124", "Ostatní dotace SFDI",
                    "127D77500", "Podpora kombinované dopravy", "327530",
                    "Dotace pro společné programy (projekty) EU a ČR", "Z270102000000",
                    "Odstraňování dopravních závad na silnicích", "327162", "Podpora rozvoje vodních cest", "327520",
                    "Výstavba a obnova silnic na území Plzně", "327152",
                    "Výstavba a obnova silnic na území hl. m. Prahy", "327151",
                    "Odstranění škod způsobených povodněmi 2000", "327183", "Rozvoj sítě dobíjecích stanic",
                    "127D77231", "R 48 Bělotín - Frýdek Místek - Český Těšín", "327126",
                    "Rekonstrukce modernizace a opravy dálnice D11 Praha -H.Králové -Polsko", "327295",
                    "Výstavba D47 Bílovec - Ostrava Rudná - Hrušov", "327242",
                    "Další výdaje spojené s dopravní politikou státu - národní dotace", "Z270202000000",
                    "Podpora výstavby a obnovy budov a staveb České správy letišť", "327512",
                    "Rozvoj a obnova materiálně-technické základny Ředitelství silnic a dálnic", "127V02300",
                    "Výstavba D11 Libice nad Cidlinou - Hradec Králové", "327252",
                    "Modernizace traťového úseku Úvaly - Česká Třebová", "327312",
                    "Výstavba a obnova mostů a silnic II. třídy (povodně EIB)", "327139",
                    "Opravy mimokoridorových celostátních tratí", "327375", "R 6 Praha - Karlovy Vary - Sokolov - Cheb",
                    "327122", "Výstavba a obnova podchodů a železničních přejezdů", "327351",
                    "Výstavba a obnova silnic na území Ostravy", "327154",
                    "Rekonstrukce modernizace a opravy dálnice D8 Praha -Ústí n.Labem -SRN", "327292",
                    "Výstavba D8 Trmice - SRN", "327225", "Rozšíření dálnice D 1 v úseku Kývalka - Holubice",
                    "327V28700", "Silniční přivaděče D5 - Plzeň", "227215", "Výstavba a obnova silnic na území Brna",
                    "327153", "R 10 + R 4 Turnov - Praha - křiž.sil.I/20 Nová Hospoda", "327121",
                    "R 7 Praha - Slaný - Louny - Chomutov", "327123", "Výstavba a obnova místních komunikací", "327170",
                    "Dálniční obchvat Plzně", "227213", "Inteligentní dopravní systémy", "327-227012",
                    "Pomoc státu při odstraňování škod vzniklých povodní v roce 2006", "327-227527",
                    "Pořízení nízko-emisních pohonných jednotek", "127D55200", "Výstavba D8 Lovosice - Řehlovice",
                    "327V22300", "Výstavba a obnova mostů a silnic I. třídy (povodně EIB)", "327119",
                    "Výstavba obchvatů sídel a nových silnic III.třídy", "327141", "CEF", "127D77400",
                    "Podpora pořízení a obnovy hnacích železničních kolejových vozidel", "327613",
                    "Inovační technologie nových linek kombinované dopravy", "227D53300",
                    "Modernizace traťového úseku st. hranice - Děčín - Praha Bubeneč", "327311",
                    "Pořízení a modernizace železničních kolejových vozidel", "127D63200",
                    "Rekonstrukce modernizace a opravy dálničního okruhu Prahy", "327297",
                    "Výstavba D47 Hrušov - Bohumín - Polsko", "327243", "Výstavba D47 Lipník nad Bečvou - Bílovec",
                    "327241", "Další výdaje spojené s dopravní politikou státu - dotace spolufinancované z EU",
                    "Z270203000000", "Modernizace plavidel vedoucí jke zvýšení bezpečnosti vnitrozemské plavby",
                    "127D55400", "Modernizace plavidel za účelem zvýšení multimodality nákladní přepravy", "127D55300",
                    "Modernizace traťového úseku Přerov - Česká Třebová", "327343", "Pořízení a obnova trolejbusů",
                    "327622", "Výstavba D 1 Hulín - Lipník nad Bečvou", "327283",
                    "Výstavba D11 Hradec Králové - Jaroměř", "327253", "Výstavba D3 Praha - Jílové - Tábor", "327261",
                    "Výstavba D5 Ejpovice - Sulkov (obchvat Plzně)", "327213",
                    "Výstavba a obnova mostů a silnic III. třídy (povodně EIB)", "327149",
                    "Úspory energie v resortu dopravy", "227032",
                    "Modernizace traťového úseku stát.hranice- Břeclav - Přerov", "327341",
                    "Zlepšení stavu mezinárodních silnic - projekt B", "327115",
                    "ERTMS - Evropský systém řízení železniční dopravy", "127D33200",
                    "Implementace subsystému kolejová vozidla – lokomotivy a kolejová vozidla pro přepravu osob – systém",
                    "127D77212",
                    "Implementace subsystému kolejová vozidla – lokomotivy a kolejová vozidla pro přepravu osob – umožnění provozu na systému 25 kV/50 Hz",
                    "127D77213", "Implementace subsystému telematika - TSI telematické aplikace v nákladní dopravě",
                    "127D33300", "Inteligentní dopravní systémy", "227012",
                    "Modernizace traťového úseku Přerov - Petrovice u Karviné - stát.hranice", "327342",
                    "Opatření 2.2 OP-Infrastruktura (Podpora rozvoje veřejných překladišť)", "327-227532",
                    "Podpora pořízení a obnovy osobních železničních kolejových vozidel", "327611",
                    "Pořízení a obnova elektrických kolejových vozidel", "327621",
                    "Projekty financované prostřednictvím OPI probíhající v letech 2007 a 2008", "327-227529",
                    "R 48 MÚK Bělotín - MÚK Rychaltice", "127V13300", "R 48 Rychaltice - Dobrá", "127V13400",
                    "Rozšíření D 1 úseku Praha - Mirošovice", "327281", "Traťové radiokomunikační systémy", "327352",
                    "Výstavba D 1 Vyškov - Hulín", "327282", "Výstavba D8 Lovosice - Řehlovice", "327223",
                    "Výstavba D8 Nová Ves - Doksany - Lovosice", "327222",
                    "Výstavba trasy metra IV.C ČD nádraží Holešovice - Letňany", "327413",
                    "Inteligentní dopravní systémy", "227D01200",
                    "Modernizace traťového úseku Brno - Břeclav - stát. hranice", "327314",
                    "Modernizace traťového úseku Česká Třebová - Brno", "327313", "OPD 2014–2020 – SFDI", "127D77300",
                    "Ostatní dotace pro SFDI", "127D76000", "Podpora obnovy pražského metra po povodni 2002", "227814",
                    "Rekonstrukce modernizace a opravy R35 Olomouc -Lipník n.Bečvou", "327293",
                    "Rekultivace odvody za odnětí půdy a výkupy pozemků na dokonč.stavbách D 1", "327286",
                    "Rozvoj a obnova mat.tech.základny Centra služeb pro silniční dopravu", "22701I",
                    "Výstavba D5 Sulkov (Plzeň) - Rozvadov - SRN", "327212",
                    "Výstavba R11 Jaroměř - státní hranice ČR/Polsko", "327255",
                    "Výstavba R35 Křelov (Olomouc) - Slavonín - Přáslavice", "327231",
                    "Výstavba silničního okruhu Třebonice -Ruzyně", "327271",
                    "Doplnění pohotovostních rezerv v dopravě", "227V02200", "Koupě části závodu", "127D38200",
                    "Majetkové vypořádání pozemků pod stavbami dálnic a silnic I.třídy", "127V02200",
                    "Modernizace a dovybavení technické základny SŽDC", "127D37300", "Odstranění povodňových škod 1998",
                    "327382", "Podpora rozvoje a obnovy letecké dopravní infrastruktury", "227510",
                    "Podpora výstavby a obnovy silnic II. a III. třídy", "227110",
                    "Podpora zavádění alternativních paliv", "227033",
                    "Pořízení a modernizace železničních kolejových vozidel – OPD 2", "127D65300",
                    "Rekonstrukce modernizace a opravy dálnice D3 Praha -Č.Budějovice -Rakousko", "327296",
                    "Rekultivace odvody za odnětí půdy a výkupy pozemků na dokonč.stavbách D 11", "327254",
                    "Rekultivace odvody za odnětí půdy a výkupy pozemků na dokonč.stavbách D 47", "327V24400",
                    "Rekultivace odvody za odnětí půdy a výkupy pozemků na dokonč.stavbách D 5", "327214",
                    "Rekultivace odvody za odnětí půdy a výkupy pozemků na dokonč.stavbách D 8", "327226",
                    "Rozvoj a obnova materiálně technické základy SFDI", "127D01900",
                    "Rozvoj a obnova materiálně-technické základny Centra služeb pro silniční dopravu", "127V02400",
                    "Rozvoj a obnova materiálně-technické základny SFDI", "127D06900",
                    "Rozvoj infrastruktury LNG plnících stanic", "127D77233",
                    "Rozvoj infrastruktury vodíkových plnících stanic", "127D77234",
                    "Výstavba D3-Tábor - Soběslav - Č.Budějovice", "327262", "Výstavba D47 Lipník nad Bečvou - Bílovec",
                    "327V24100", "Výstavba D5 Praha - Ejpovice (Plzeň)", "327211",
                    "Výstavba R35 Přáslavice - Velký Újezd - Lipník nad Bečvou", "327232",
                    "Výstavba SO kolem hlavního města Prahy část jihozápadní", "227270",
                    "Výstavba cyklistických stezek", "327161", "Výstavba obchvatů sídel a nových silnic I.třídy",
                    "327-327111", "Výstavba silničního okruhu Ruzyně -Březiněves", "327272",
                    "Úspory energie v resortu dopravy", "227D03200",
                    "Implementace subsystému kolejová vozidla - nákladní vozy – splnění hlukových požadavků dle technických specifikací interoperability subsystémů hluk a nákladní vozy",
                    "127D77215", "Ostatní výdaje spojené s dopravní politikou státu agregace", "Z270201000000",
                    "Přepravní jednotky kombinované dopravy", "127D77241",
                    "Rekonstrukce a modernizace regionálních tratí", "327322",
                    "Rekul. odvody za odnětí půdy a výkupy pozemků na dokonč.stavbách okruhu Prahy", "327277",
                    "Rekultivace odvody za odnětí půdy a výkupy pozemků na dokonč.stavbách R35", "327233",
                    "Silnice I/27 Litice - Šlovice", "227214", "Výstavba D8 Praha (Zdiby) - Úžice - Nová Ves", "327221",
                    "Výstavba R 49 Holešov - SR", "327285", "Výstavba R 55 Hulín - Skalka", "327284",
                    "Výstavba a technická obnova staveb MHD", "327430", "Výstavba pražského metra", "127D41200",
                    "Výstavba silničního okruhu Březiněves-Satalice", "327276",
                    "Výstavba silničního okruhu D1-Českobrodská-Satalice", "327275",
                    "Výstavba silničního okruhu Třebonice -Vestec", "327273", "Výstavba silničního okruhu Vestec-D1",
                    "327274", "Výstavba technického centra a depa Zličín trasy B metra", "327412",
                    "Racionalizace spotřeby a využití obnovitelných zdrojů energi", "227019",
                    "Podpora řešení úkolů vědy a výzkumu", "327920",
                    "Reprodukce investičního majetku Centra dopravního výzkumu", "327911",
                    "Výdaje spojené s kosmickými aktivitami", "Z270204000000"
                }
            },
            {
                CalculatedCategories.Energetika, new[]
                {
                    "EFEKT - podpora úspor energie", "122D14200", "Přechod na čistší zdroje energie", "Z220304000000",
                    "Podpora strategie v oblasti zvyšování energetické účinnosti", "122D22200",
                    "Investiční podpora realizace energeticky úsporných projektů", "122D22100",
                    "Podpora opatření k úsporám energií", "322040",
                    "Program podpory na zvýšené náklady na zemní plyn a elektřinu v důsledku mimořádně prudkého růstu jejich cen agregace",
                    "Z224301000000", "Snižování spotřeby energie", "Z220310000000", "EFEKT - podpora úspor energie",
                    "322-122142", "Podpora ke zvýšení účinnosti užití energie", "222043", "Program EFEKT 3 agregace",
                    "Z221901000000", "7.3 Komplexní reforma poradenství týkající se renovační vlny v ČR (REPowerEU)",
                    "Z220321000000", "Podpora ke zvýšení účinnosti užití energie", "322-222043",
                    "Podpora výrobních a rozvodných zařízení energie", "222042",
                    "Podpora obnovy energetických liniových staveb po povodni 2002", "222812",
                    "Podpora vyššího využití obnovitelných zdrojů energie - OBNOVITELNÉ ZDROJE ENERGIE", "322-22221B",
                    "Podpora snižování energetické náročnosti - ÚSPORY ENERGIE", "322-22221A",
                    "Podpora výrobních a rozvodných zařízení energie", "322-222042",
                    "Financování opatření podle NV č. 5/2023 Sb. o kompenzacích poskytovaných na dodávku elektřiny a plynu za stanovené ceny. agregace",
                    "Z224601000000",
                    "Financování opatření podle NV č. 463/2022 Sb. na úhradu prokazatelné ztráty a přiměřeného zisku obchodníků s elektřinou a plynem z dodávky elektřiny a plynu na ztráty v distribučních soustavách. agregace",
                    "Z224401000000", "Dotace ČEPS na ztráty pro rok 2023", "Z224701000000",
                    "Podpora snižování energetické náročnosti - ÚSPORY ENERGIE", "22221A",
                    "Dotace ČEPS na systémové služby pro rok 2023", "Z224702000000",
                    "Podpora vyššího využití obnovitelných zdrojů energie - OBNOVITELNÉ ZDROJE ENERGIE", "22221B",
                    "Příspěvek na úhradu nákladů za energie agregace", "Z224101000000",
                    "Dotace na obnovitelné zdroje energie agregace", "Z223201000000",
                    "Racionalizace spotřeby a využití obnovitelných zdrojů energií", "233349",
                    "Snížení energetické náročnosti staveb a racionalizace spotřeby paliv a energií", "333217",
                    "Racionalizace spotřeby a využití obnovitelných zdrojů energií", "233D34900",
                    "Racionalizace spotřeby a využití obnovitelných zdrojů energií", "233D31900",
                    "Výstavba a obnova tepelně-energetckých systémů a inženýrských sítí", "333315",
                    "Výstavba a obnova tepelně-energetických systémů a inženýrských sítí", "333214",
                    "Snížení energetické náročnosti staveb a racionalizace spotřeby paliv a energií", "333117",
                    "Výstavba a obnova tepelně-energetických systémů a inženýrských sítí", "333114",
                    "Racionalizace spotřeby a využití obnovitelných zdrojů energií", "333-233319",
                    "Racionalizace spotřeby a využití obnovitelných zdrojů energií", "333-233349",
                    "Racionalizace spotřeby a využití obnovitelných zdrojů energií", "233329",
                    "Snížení energetické náročnosti staveb a racionalizace spotřeby paliv a energií", "333318",
                    "Zpracování energetických auditů stávajících objektů", "314431",
                    "Racionalizace spotřeby a využití obnovitelných zdrojů energií", "214049", "214029",
                    "Racionalizace spotřeby paliv a energií", "235019",
                    "Racionalizační opatření pro snížení spotřeby energií státních léčebných ústavů", "235139",
                    "Racionalizační opatření pro snížení spotřeby energií státních léčebných ústavů", "235V13900",
                    "Racionalizace spotřeby paliv a energií", "235V01900",
                    "Racionalizace spotřeby a využití obnovitelných zdrojů energi", "227019"
                }
            },
            {
                CalculatedCategories.HumanitarniPomoc, new[]
                {
                    "Státní pomoc při obnově území postiženého povodní 2002 poskytovaná MPSV", "213810",
                    "Dotace na úhradu výdajů sociálních služeb souvisejících s pomocí osobám z území Ukrajiny agregace",
                    "Z131501000000",
                    "Mimořádná dotace pro dočasné aktivity na podporu rodin z Ukrajiny v rámci center pro rodiny s dětmi agregace",
                    "Z131701000000",
                    "Výdaje spojené se zajištěním provozu a činností Krajských asistenčních center pomoci Ukrajině",
                    "Z142901000000", "Evropský uprchlický fond", "314-214015",
                    "Odstranění škod způsobených povodní 1997 - MV", "314080", "Zahraniční rozvojová pomoc",
                    "314-214014", "Rozvoj a obnova mat tech základny uprchlických zařízení", "214042",
                    "Výdaje spojené se zajištěním peněžních náhrad za poskytnutí pomoci", "Z143101000000",
                    "Výdaje spojené se zajištěním peněžních náhrad - NP České Švýcarsko agregace", "Z143101000000",
                    "Státní podpora humanitárních projektů", "335040", "Podpora humanitárních projektů", "235D32300",
                    "Program švýcarsko-české spolupráce", "135D32P00", "EHP Norsko", "135D32R00",
                    "Podpora humanitárních projektů", "235323", "Podpora humanitárních projektů", "335-235323",
                    "Financování Českého červeného kříže (tzv. institucionální příspěvek) agregace", "Z351801000000",
                    "Humanitární dotace agregace", "Z060101000000", "DRR a odolnost", "Z060102000000",
                    "Projekty NNO Programu TS - Ukrajina", "Z060307000000", "Humanitární a stabilizační pomoc Ukrajině",
                    "Z060106000000"
                }
            },
            {
                CalculatedCategories.InovaceVyzkum, new[]
                {
                    "Pořízení a technická obnova invest.majetku organizací vědy a výzkumu", "313910",
                    "Dlouhodobý koncepční rozvoj výzkumných organizací agregace", "Z131301000000",
                    "Program aplikovaného výzkumu MZe \"Země\" (2017-2025) agregace", "Z291201000000",
                    "Program na podporu aplikovaného výzkumu MZe \"Země II\" (2024-2032) agregace", "Z291401000000",
                    "Dlouhodobý koncepční rozvoj výzkumných organizací agregace", "Z291101000000",
                    "Operační program Technologie a aplikace pro konkurenceschopnost agregace (dotace mimo VVI)",
                    "Z220402000000", "Komponenta 2.5.3 Předprojektová příprava a osvěta", "Z220317000000",
                    "Operační program Podnikání a inovace pro konkurenceschopnost agregace pro VVI", "Z220601000000",
                    "program The Country for the Future (2020-2027) agregace", "Z222601000000",
                    "Zavádění nových technologií a výrobků do výrobní základny", "322081",
                    "program TRIO (2016-2022) agregace", "Z222501000000",
                    "Podpora inovací výrobků technologií a služeb - INOVACE", "322-222219",
                    "Pořízení a tech. obnova invest. majetku k řešení úkolů vědy a výzkumu", "322920",
                    "Operační program Technologie a aplikace pro konkurenceschopnost agregace pro VVI", "Z220401000000",
                    "Podpora infrastruktury pro průmyslový výzkum vývoj a inovací - PROSPERITA", "322-222215",
                    "Dlouhodobý koncepční rozvoj výzkumných organizací agregace", "Z222401000000",
                    "Program Czech Rise Up 2.0 - Výzkum proti COVID -19 agregace", "Z221801000000",
                    "Podpora inovací výrobků technologií a služeb - INOVACE", "222219",
                    "Program podpory mezinárodní technologické spolupráce", "122D15200",
                    "Podpora infrastruktury pro průmyslový výzkum vývoj a inovací - PROSPERITA", "222215",
                    "Podpora rozvoje vědeckotechnických center", "222213", "Rozvoj a obnova mat.tech. základny ČMI",
                    "122V01500", "Rozvoj a obnova mat.tech.základny ČMI", "22201E",
                    "Rozvoj a obnova mat.tech.základny Czechinvest", "22201K", "Rozvoj a obnova mat.tech.základny ČMI",
                    "222V01E00", "Systémová podpora implementace a řízení Národní RIS3 strategie 2023+",
                    "Z225002000000",
                    "OP Výzkum a vývoj pro inovace", "CZ.1.05", "Inter-Excellence II agregace", "Z332301000000",
                    "Podpora výzkumu a vývoje", "333910", "Inter-Excellence agregace", "Z332201000000",
                    "Projekty velkých infrastruktur pro VaVaI agregace", "Z331901000000",
                    "Pořízení a obnova invest.majetku zabezpečujícího úkoly vědy a výzkumu", "333912",
                    "9B Evropské partnerství v oblasti metrologie agregace", "Z335001000000",
                    "Dlouhodobý koncepční rozvoj výzkumných organizací agregace", "Z330701000000",
                    "Evropský metrologický program pro inovace a výzkum agregace", "Z331101000000",
                    "Makroregionální spolupráce ve výzkumu vývoji a inovacích agregace", "Z331401000000",
                    "Společná technologická iniciativa ECSEL agregace", "Z331001000000",
                    "Reprodukce investičního majetku organizací vědeckovýzkumné základny", "333911",
                    "Program pro financování projektů mnohostranné vědeckotechnické spolupráce v Podunajském regionu agregace",
                    "Z331701000000", "EuroHPC projekty agregace", "Z331801000000",
                    "9F Makro-regionální spolupráce ve výzkumu vývoji a inovacích agregace", "Z335301000000",
                    "Horizont 2020 - rámcový program pro výzkum a inovace agregace", "Z331201000000",
                    "Evropská zájmová skupina pro spolupráci s Japonskem agregace", "Z331501000000",
                    "Eurostars agregace", "Z330901000000",
                    "Program podpory excelentního výzkumu v prioritních oblastech veřejného zájmu ve zdravotnictví EXCELES agregace",
                    "Z332401000000", "Projekty sdílených činností agregace", "Z332001000000",
                    "LV - Program Dioscuri agregace", "Z335501000000",
                    "9D Doplňkový program Evropského společenství pro atomovou energii (EURATOM) pro výzkum a odbornou přípravu (2021–2025) agregace",
                    "Z335101000000",
                    "9G Podpora vědeckých výměn mezi Českou republikou a Spojenými státy americkými agregace",
                    "Z335201000000",
                    "Otevřené výzvy v bezpečnostním výzkumu 2023-2029 (OPSEC) agregace", "Z142101000000",
                    "Program bezpečnost výzk ČR: vývoj testování SECTECH (2021-2026) agregace", "Z142001000000",
                    "Program IMPAKT 1 - Strateg podpora rozvoje bezpečnost výzk (2019-2025) agregace", "Z141901000000",
                    "Program bezpečnostního výzkumu pro potřeby státu 2022-2027 (SecPro) agregace", "Z142201000000",
                    "Program institucionální podpory Ministerstva vnitra", "Z141701000000",
                    "Resortní program výzkumu V. (2020-2026) agregace", "Z350401000000",
                    "Program na podporu zdravotnického aplikovaného výzkumu na léta 2024 - 2030 agregace",
                    "Z352301000000", "Dlouhodobý koncepční rozvoj výzkumných organizací agregace", "Z350201000000",
                    "Pořízení a tech. obnova invest. majetku k řešení úkolů vědy a výzkumu", "335920",
                    "Resortní program výzkumu IV. (2015-2022) agregace", "Z350301000000",
                    "Pořízení a technická obnova invest.majetku organizací vědy a výzkumu", "361910",
                    "Pořízení a tech. obnova invest. majetku k řešení úkolů vědy a výzkumu", "361920",
                    "Odstranění škod způsobených povodní 1997 - AV", "361180",
                    "Podpora řešení úkolů vědy a výzkumu", "327920",
                    "Reprodukce investičního majetku Centra dopravního výzkumu", "327911",
                    "Výdaje spojené s kosmickými aktivitami", "Z270204000000",
                    "Dlouhodobý koncepční rozvoj výzkumných organizací agregace", "Z060701000000",
                    "Pořízení a tech. obnova invest. majetku k řešení úkolů vědy a výzkumu", "306920"
                }
            },
            {
                CalculatedCategories.KulturaMedia, new[]
                {
                    "Program COVID KULTURA agregace", "Z221601000000",
                    "Kulturní aktivity - OU", "Z340807000000", "Kulturní aktivity - OLK", "Z340808000000",
                    "Záchrana architektonického dědictví", "434D31200", "Veřejné informační služby knihoven agregace",
                    "Z341001000000", "Kulturní aktivity - ORNK", "Z340812000000",
                    "Rozvoj informačních sítí veřejných knihoven", "334012", "Národní plán obnovy - OKKO",
                    "Z342804000000", "Integrovaný systém ochrany movitého kulturního dědictví", "434313",
                    "ISO/D -Preventivní ochrana před nepříznivými vlivy prostředí", "134D51500",
                    "Havarijní program agregace", "Z341501000000", "Národní plán obnovy - OU", "Z342801000000",
                    "Program záchrany architektonického dědictví agregace", "Z341401000000",
                    "Program regenerace městských památkových rezervací a městských památkových zón agregace",
                    "Z341701000000", "Záchrana architektonického dědictví", "434312",
                    "Podpora obnovy kulturních památek prostřednictvím obcí s rozšířenou působností agregace",
                    "Z342001000000",
                    "ISO/A - Zabezpečení objektů, v nichž jsou uloženy předměty movitého kulturního dědictví, bezpečnostn",
                    "134D51200", "Podpora reprodukce majetku státních kulturních zařízení", "134V11200",
                    "Program restaurování movitých kulturních památek agregace", "Z341601000000",
                    "Podpora reprodukce majetku církví a náboženských organizací- alokace PSP", "234412",
                    "Integrovaný systém ochrany movitého kulturního dědictví", "434D31300",
                    "D – Preventivní ochrana před nepříznivými vlivy prostředí", "134D52400",
                    "Integrovaný systém ochrany movitého kulturního dědictví", "334-434313", "Kulturní aktivity - OMA",
                    "Z340805000000", "Národní plán obnovy - OMV", "Z342802000000",
                    "Péče o vesnické památkové rezervace a zóny a krajinné památkové zóny agregace", "Z341801000000",
                    "Záchrana architektonického dědictví", "334030", "Kulturní aktivity - OMV", "Z340803000000",
                    "Podpora reprodukce majetku církví a náboženských organizací- alokace PSP", "334-234412",
                    "Zabezpečení objektů systémy EZS,EPS,a mechanickými zábranami", "334021",
                    "Integrovaný systém ochrany movitého kulturního dědictví", "334020",
                    "A – Zabezpečení objektů, v nichž jsou uloženy předměty movitého kulturního dědictví, bezpečnostními",
                    "134D52100", "Podpora reprodukce majetku regionálních kulturních zařízení-alokace PSP",
                    "334-234212", "Integrovaný systém ochrany movitého kulturního dědictví", "234313",
                    "Subtitul pro ochranu měkkých cílů v oblasti kultury pro ostatní žadatele", "134D81100",
                    "Podpora reprodukce majetku státních kulturních zařízení", "234V11200",
                    "Podpora reprodukce majetku státních kulturních zařízení", "234112",
                    "Podpora reprodukce majetku regionálních kulturních zařízení-alokace PSP", "234212",
                    "Podpora kulturních aktivit národnostních menšin agregace", "Z342201000000",
                    "Záchrana architektonického dědictví", "234312",
                    "Program aplik. VaV národní a kulturní identity - NAKI III (2023-2030) agregace", "Z340201000000",
                    "Kulturní aktivity - SOCNS", "Z340806000000",
                    "Program státní podpory profesionálních divadel a stálých profesionálních symfonických orchestrů a pěveckých sborů agregace",
                    "Z340701000000", "Akviziční fond", "134D53100",
                    "ISO/A - Zabez. obj., v nichž jsou uloženy před. mov. kult. dědictví, bezp. sys. a mech. zábranami",
                    "134V51200", "Podpora expozičních a výstavních projektů agregace", "Z341201000000",
                    "Program státní podpory festivalů profesionálního umění agregace", "Z340901000000",
                    "Podpora reprodukce majetku církví a náboženských organizací- alokace PSP", "234D41200",
                    "ISO/C -Výkupy předmětů kulturní hodnoty mimořádného významu", "134D51400",
                    "Podpora výchovně vzdělávacích aktivit v muzejnictví agregace", "Z341301000000",
                    "Reprodukce investičního majetku v působnosti MK", "334010",
                    "ISO/D - Preventivní ochrana před nepříznivými vlivy prostředí", "134V51500",
                    "Podpora reprodukce majetku regionálních kulturních zařízení-alokace PSP", "234D21200",
                    "ISO/B - Evidence a dokumentace movitého kulturního dědictví", "134V51300",
                    "Kulturní aktivity - SOEU", "Z340804000000",
                    "Podpora rozšiřování a přijímání informací v jazycích národnostních menšin agregace",
                    "Z342301000000", "Podpora standardizovaných veřejných služeb muzeí a galerií agregace",
                    "Z341101000000", "Program podpory pro památky světového dědictví agregace", "Z341901000000",
                    "Podpora projektů integrace příslušníků romské komunity agregace", "Z342101000000",
                    "C – Výkupy předmětů kulturní hodnoty mimořádného významu", "134D52300",
                    "ISO/C - Výkupy předmětů kulturní hodnoty mimořádného významu", "134V51400",
                    "Národní plán obnovy - SOM", "Z342806000000", "Program mobility pro všechny", "134D21500",
                    "Obnova majetku státních kulturních zařízení poškozeného povodní 2002", "234118",
                    "Kulturní aktivity - OPP", "Z340809000000",
                    "Podpora reprodukce majetku regionálních kulturních zařízení", "134D21200",
                    "Dlouhodobý koncepční rozvoj výzkumných organizací agregace", "Z340301000000",
                    "Podpora obnovy majetku regionálních kulturních zařízení po povodni 2002", "234812",
                    "Výkupy kulturních památek a předmětů kulturní hodnoty", "334023",
                    "Náhrada objektů vydaných v restitucích", "334070", "Kulturní aktivity - SOOKS", "Z340811000000",
                    "Integrovaný operační program", "CZ.1.06", "Kulturní aktivity - SOM", "Z340810000000",
                    "Podpora realizace úsporných energetických zařízení", "134V11500",
                    "Pořízení a tech. obnova invest. majetku k řešení úkolů vědy a výzkumu", "334920",
                    "Národní plán obnovy - OLK - Status umělce", "Z342807000000",
                    "ISO/B -Evidence a dokumentace movitého kulturního dědictví", "134D51300",
                    "Podpora reprodukce majetku církví a náboženských organizací", "134D41200",
                    "Podpora reprodukce majetku státních kulturních zařízení", "334-234112",
                    "Obnova a rozvoj materiální základny národních kulturních institucí", "134V12400",
                    "Reprodukce inv.majetku muzeí a galerií", "334011", "Integrovaný regionální operační program",
                    "Podpora obnovy kulturních památek v majetku krajů a obcí", "134D61200",
                    "ISO/E Uplatnění předkupního práva státu podle § 13 zákona č. 20/1987 Sb., o státní památkové péči",
                    "134V51700", "Podpora realizace úsporných energetických opatření", "334-234115",
                    "Praha-evropské město kultury 2000", "334013", "Program mobility pro všechny", "234D21500",
                    "B – Evidence a dokumentace movitého kulturního dědictví", "134D52200",
                    "Náhrada úbytku objektů kulturních zařízení", "234113",
                    "Obnova a rozvoj materiální základny Národní knihovny ČR", "134V12300",
                    "Rekonstrukce a opravy církevních objektů", "334111",
                    "Rehabilitace památníků bojů za svobodu,nezávislost a demokracii", "334120",
                    "Náhrada úbytku objektů kulturních zařízení", "134V11300",
                    "Podpora obnovy kulturních památek v majetku státu", "134V61100",
                    "Podpora realizace úsporných energetických opatření", "234V11500", "Program mobility pro všechny",
                    "134D22200", "Obnova a rozvoj materiální základny Národního muzea", "134V12200",
                    "Státní pomoc při obnově území postiženého povodní 2006", "334-234218",
                    "F –Realizace nároků na navracení nezákonně vyvezeného movitého kulturního dědictví", "134D52600",
                    "Obnova nemovitých kulturních památek", "334040",
                    "Rehabilitace památníků za svobodu, nezávislost a demokracii", "234314",
                    "Podpora reprodukce majetku regionálních kulturních zařízení , církví a náboženských společností",
                    "134D22100", "OP Vzdělávání pro konkurenceschopnost", "CZ.1.07",
                    "Podpora obnovy kulturních památek v majetku fyzických osob", "134D61300",
                    "Podpora obnovy majetku církví a náboženských organizací poškozeného povodní 2002", "234418",
                    "Program mobility pro církve a náboženské organizace", "134D41500",
                    "Reprodukce majetku ve správě MK", "334050",
                    "Centrum pro dokumentaci majetkových převodů kulturních statků obětí 2. světové války agregace",
                    "Z340401000000", "Nadační fond obětem holocaustu agregace", "Z340501000000",
                    "Národní plán obnovy - OE/ OI", "Z342805000000",
                    "Státní fond kinematografie - dotace na poskytování podpory kinematografie agregace",
                    "Z342601000000", "Státní fond kinematografie - dotace ze SR agregace", "Z342501000000",
                    "Státní fond kinematografie - dotace ze SR účelově určená na filmové pobídky agregace",
                    "Z342401000000", "Státní fond kultury České republiky agregace", "Z342701000000",
                    "Náhrada úbytku objektů kulturních zařízení", "234V11300",
                    "Obnova a rozvoj materiální základny Národního muzea", "334-134122",
                    "Podpora obnovy kulturních památek v majetku církví a náboženských společností", "134D61400",
                    "Podpora obnovy majetku církví a náboženských organizací poškozeného povodní 2002", "334-234418",
                    "Podpora záchrany, obnovy a revitalizace kulturních památek - EHP/No", "334-234114",
                    "Zpracování kulturního dědictví a aktuálních témat s Německem Rakouskem a Lichtenštejnskem agregace",
                    "Z060401000000",
                    "Kultura agregace", "Z120201000000", "Památník Šoa Praha o.p.s.", "Z980508000000",
                    "Platforma evropské paměti a svědomí", "Z980505000000",
                    "Spolek pro zachování odkazu českého odboje", "Z980506000000", "Ústav TGM o.p.s.", "Z980504000000",
                    "Československá obec legionářská", "Z980502000000"
                }
            },
            {
                CalculatedCategories.MaleStredniPodniky, new[]
                {
                    "Program podpory malých podniků postižených celosvětovým šířením onemocnění COVID-19 způsobeného virem SARS-CoV-19 - \"OŠETŘOVNÉ\" PRO OSVČ agregace",
                    "Z221301000000", "OBCHŮDEK 2021+ agregace", "Z220201000000",
                    "Komunitární program COSME - Business Innovation Support Network 3 agregace", "Z223301000000",
                    "INOSTART agregace", "Z222001000000", "Internacionalizace začínajících podniků", "Z220319000000"
                }
            },
            {
                CalculatedCategories.ObranaBezpecnost, new[]
                {
                    "Dotace na výdaje jednotek sborů dobrovolných hasičů obcí poskytovaná obcím prostřednictvím krajů agregace",
                    "Z141401000000", "Dotace pro obnovu movitého majetku", "014D24100",
                    "Dotace na pořízení nového dopravního automobilu", "014D26200",
                    "Dotace na obnovu nemovitého majetku", "014D24200",
                    "Reprodukce majetku jednotek požární ochrany a ochrany obyvatelstva", "314-214214",
                    "Reprodukce investičního majetku hasičských záchranných sborů okresů", "314621",
                    "Reprodukce majetku jednotek požární ochrany a ochrany obyvatelstva", "114D21400",
                    "Reprodukce majetku jednotek požární ochrany a ochrany obyvatelstva", "214214",
                    "Podpora NNO působících na úseku PO IZS ochrany obyvatelstva a krizového řízení agregace",
                    "Z140101000000", "Pořizování a obnova majetku jednotek požární ochrany obyvatelstva", "114D24400",
                    "Dotace na stavbu nebo rekonstrukci požární zbrojnice", "014D26300",
                    "Dotace na pořízení nebo rekonstrukci cisternové automobilové stříkačky", "014D26100",
                    "Zabezpečení krajských sídel", "314041", "Reprodukce investičního majetku civilní ochrany",
                    "314623", "Pořízení a obnova majetku obcí na řešení bezpečnostních rizik v souvislosti s migrací",
                    "314D09200", "Pořízení a modernizace majetku VZS ČČK", "014D25200",
                    "Reprodukce investičního majetku jednotek požární ochrany", "314622",
                    "Podpora spolkové činnosti spolků působících na úseku požární ochrany", "Z140401000000",
                    "Pořízení a obnova majetku obcí na řešení bezpečnostních rizik", "314D10200",
                    "Reprodukce majetku jednotek požární ochrany a ochrany obyvatelstva", "214D21400",
                    "PEGAS - síť Jižní Morava", "314127", "PEGAS - síť Jižní Čechy", "314123",
                    "PEGAS - síť Severní Morava", "314128", "PEGAS - síť Severní Čechy", "314125",
                    "PEGAS - síť Východní Čechy", "314126", "PEGAS - síť Západní Čechy", "314124", "PEGAS - síť Praha",
                    "314121", "PEGAS - síť Střední Čechy", "314122",
                    "Reprodukce nemovitého majetku organizačních útvarů systému řízení MV", "214V01200",
                    "Investiční dotace pro nestátní neziskové organizace na úseku PO", "014D27100",
                    "Podpora akceschopnosti Vodní záchranné služby ČČK zs agregace", "Z140301000000",
                    "Veřejně prospěšný program v oblasti tělesné a střelecké přípravy příslušníků Policie ČR",
                    "Z141101000000", "Výstavba HZS vyšších územně správních celků (VÚSC)", "314612",
                    "Veřejně prospěšný program v oblasti tělesné a střelecké přípravy příslušníků Policie ČR",
                    "Z141101000000", "Výstavba HZS vyšších územně správních celků (VÚSC)", "314612",
                    "Biologická ochrana obyvatelstva", "235223"
                }
            },
            {
                CalculatedCategories.CestovniRuchPamatky, new[]
                {
                    "Podpora pro vytvoření nebo obnovu místa pasivního odpočinku", "129D66500",
                    "Údržba a obnova významných zemědělských historických dominant jedinečného charakteru a hasičských zb",
                    "129D66300", "Podpora údržby a obnovy historických zemědělských strojů a zařízení", "129D66400",
                    "Rozvoj základní a doprovodné infrastruktury cestovního ruchu", "117D72100",
                    "Cestování dostupné všem", "117D71300", "Obnova drobných sakrálních staveb a hřbitovů", "117D8210G",
                    "Marketingové aktivity v cestovním ruchu", "117D72200",
                    "Podpora budování doprovodné inftastruktury cestovního ruchu pro sportovně-rekreační aktivity",
                    "217212", "Podpora rozvoje měst a obcí s lázeňským statutem", "317321", "Cestovní ruch pro všechny",
                    "117D71200", "Podpora prezentace České republiky jako destinace cestovního ruchu", "217214",
                    "COVID-Lázně agregace", "Z171001000000", "Podpora rozvoje lázeňství", "217215",
                    "Podpora vlastníků lázeňské infrastruktury", "317322",
                    "Podpora prezentace České republiky jako destinace cestovního ruchu", "317327",
                    "Rozšiřování ubytovací kapacity v kategorii ubytování v soukromí", "317323",
                    "COVID-Ubytování agregace", "Z170901000000",
                    "Podpora budování doprovodné infrastruktury cest. ruchu na území měst. památkových rezervací a zón",
                    "317326", "NNV - NNO v oblasti cestovního ruchu", "Z170605000000",
                    "Podpora budování doprovodné inftastruktury cestovního ruchu pro sportovně-rekreační aktivity",
                    "317-217212", "Rozvoj městské turistiky", "317324",
                    "Podpora základní činnosti Horské služby ČR agregace", "Z170401000000", "Podpora rozvoje lázeňství",
                    "317-217215", "COVID-Cestovní kanceláře II agregace", "Z170701000000",
                    "COVID-Průvodci v cestovním ruchu agregace", "Z170801000000",
                    "Podpora návštěvnosti destinace České Švýcarsko agregace", "Z171101000000"
                }
            },
            {
                CalculatedCategories.PravniStat, new[]
                {
                    "Vnitřní věci - malé grantové schéma", "Z143601000000", "Protikorupční opatření", "314076",
                    "Národní program pro přijímání acquis communitaire", "235014",
                    "Projekty NNO Programu transformační spolupráce", "Z060302000000", "Podpora strategických partnerů",
                    "Z060306000000", "Platformy - Program transformační spolupráce", "Z060303000000",
                    "Podpora strategických partnerů - Ukrajina", "Z060308000000",
                    "Nové dotatečné apod. volby do zastupitelstev ÚSC", "Z980204000000",
                    "Prezident republiky - volby - pro ÚSC", "Z980208000000", "Evropský parlament - volby-pro ÚSC",
                    "Z980206000000", "Zastupitelstva krajů- řádné volby - pro ÚSC", "Z980203000000",
                    "Výdaje stanovené zvláštními zákony nebo dalšími právními předpisy", "Z980102000000",
                    "Spravedlnost agregace", "Z120401000000", "Řádná správa věcí veřejných agregace", "Z120101000000",
                    "Rada pro veřejný dohled nad auditem agregace", "Z120601000000", "Masarykovo demokratické hnutí",
                    "Z980503000000", "Senát - řádné volby - pro ÚSC", "Z980201000000", "Spolek Bez komunistů.cz",
                    "Z980507000000"
                }
            },
            {
                CalculatedCategories.PodporaPodnikani, new[]
                {
                    "Podpora nestátním neziskovým organizacím MZe agregace", "Z290501000000",
                    "Podpora NNO v působnosti MZe", "129D03200", "Phare", "713N01",
                    "OP Podnikání a inovace", "CZ.1.03", "Operační program Podnikání a inovace pro konkurenceschopnost",
                    "COVID 2022 – Sektorová podpora agregace", "Z221501000000", "Operační program Průmysl a podnikání",
                    "CZ.04.1.01", "Program COVID Nepokryté náklady agregace", "Z221401000000",
                    "COVID - ADVENTNÍ TRHY agregace", "Z220501000000",
                    "Podpora reprodukce majetku k realizaci procesu ZNHČ pro DIAMO s. p.", "122D16200",
                    "Podpora reprodukce majetku k realizaci procesu ZNHČ od roku 2019 – DIAMO s. p.", "122D25100",
                    "Podpora řešení důsledků útlumu těžebního průmyslu", "222113",
                    "Podpora podniků ve fázi růstu s využitím intenzifikačních faktorů rozvojem - ROZVOJ", "322-222218",
                    "Investice vyvolané útlumem uhelného rudného a uranového hornictví", "322060",
                    "Podpora rozvoje podnikání a zvyšování konkurenceschopnosti produkčního sektoru", "222222",
                    "Podpora rozvoje podnikatelské infrastruktury - REALITY", "322-222216", "Phare", "722N01",
                    "Odstranění škod způsobených povodní 1997 - MPO", "322180",
                    "Restrukturalizace průmyslové výrobní základny- SOP2", "322087",
                    "Podpora řešení důsledků útlumu těžebního průmyslu", "322-222113",
                    "Zvyšování konkurenceschopnosti průmyslové produkce-SOP3", "322088",
                    "Podpora reprodukce majetku k realizaci procesu ZNHČ od roku 2019 – Palivový kombinát Ústí s. p.",
                    "122D25200", "Podpora podnikání v průmyslu a průmyslových službách- SOP1", "322086",
                    "Podpora podniků ve fázi růstu s využitím intenzifikačních faktorů rozvojem - ROZVOJ", "222218",
                    "Podpora rozvoje podnikatelské infrastruktury - REALITY", "222216",
                    "Podpora restrukturalizace průmyslové výrobní základny", "222223",
                    "Ochrana spotřebitele - NNO agregace", "Z220101000000",
                    "Pořízení a tech. obnova invest. majetku ve správě Ministerstva průmyslu a obchodu", "322010",
                    "Podpora reprodukce majetku k realizaci procesu ZNHČ pro Palivový kombinát Ústí s. p.", "122D16300",
                    "Podpora řešení důsledků útlumu těžebního průmyslu", "222D11300",
                    "Dotace z úhrad z vydobytých nerostů agregace", "Z221001000000",
                    "Podpora akcí zabezpečujících rozvoj podniku – DIAMO s. p.", "122D25300",
                    "Dotace na mezinárodní akreditaci pro ČIA o.p.s. agregace", "Z220801000000",
                    "Strategické investiční akce - investiční pobídky agregace", "Z222101000000", "Technická část OKD",
                    "Z220903000000", "Technická část ZNHČ", "Z220902000000", "Informační místa pro podnikatele",
                    "222214", "OP TAK - Finanční nástroje", "Z220403000000", "Program COVID Nájemné agregace",
                    "Z222801000000", "Covid III agregace", "Z224201000000",
                    "Národní plán obnovy - komponenty adminstrované MPO agregace", "Z220301000000",
                    "Podpora akcí zabezpečujících rozvoj podniku – Palivový kombinát Ústí s. p.", "122D25400",
                    "Program COVID Gastro - Uzavřené provozovny agregace", "Z224001000000",
                    "Středoevropský fond fondů - převod vkladu do Fondu VC 2017 agregace", "Z222201000000",
                    "Technická pomoc SOP 2003", "222225",
                    "Operační program Průmysl a podnikání", "CZ.04.1.01",
                    "Integrovaný regionální operační program", "Podpora obnovy a rozvoje venkova", "117D81500",
                    "Společný regionální operační program", "CZ.04.1.05", "Obnova venkova", "317710",
                    "Integrovaný operační program", "CZ.1.06", "Obnova venkova-II", "317711",
                    "Výstavba nájemních bytů a technické infrastruktury ve vlastnictví obcí", "317420",
                    "Podpora obnovy venkova", "217D11500", "Podpora obnovy venkova", "317-217115",
                    "Podpora regionálního rozvoje", "317620", "Bytové domy bez bariér", "117D06600",
                    "INTERREG IIIA Česká republika - Polsko", "CZ.04.4.85", "Rekonstrukce a přestavba veřejných budov",
                    "117D8210E", "Podpora obnovy venkova", "217115", "Společný regionální operační program",
                    "317-21711A", "Podpora výstavby podporovaných bytů", "117D51400",
                    "Podpora výstavby technické infrastruktury", "117D51300",
                    "Jednotný programový dokument pro Cíl 3 regionu hl.m.Praha", "CZ.04.3.07",
                    "INTERREG IIIA Česká republika - Rakousko", "CZ.04.4.83",
                    "Podpora územně plánovacích dokumentací obcí", "117D05100",
                    "OP Přeshraniční spolupráce Slovenská republika – Česká republika 2007-2013", "CZ.3.21",
                    "JPD Cíl 2 Praha", "CZ.04.2.06", "Podpora regenerace panelových sídlišt", "117D51200",
                    "Podpora rozvoje severozápadních Čech a Moravskoslezského regionu", "217112",
                    "INTERREG IIIA Česká republika - Svobodný stát Bavorsko", "CZ.04.4.82",
                    "INTERREG IIIA Česká republika - Svobodný stát Sasko", "CZ.04.4.81",
                    "Podpora oprav vad panelové výstavby", "217315", "Podpora oprav domovních olověných rozvodů",
                    "117D51500", "OP Přeshraniční spolupráce Česká republika – Polská republika 2007-2013", "CZ.3.22",
                    "Podpora rozvoje severozápadních Čech a Moravskoslezského regionu", "317-217112",
                    "OP Přeshraniční spolupráce Svobodný stát Sasko – Česká republika 2007-2013", "CZ.3.18",
                    "OP Přeshraniční spolupráce Rakouská republika – Česká republika 2007-2013", "CZ.3.20",
                    "IROP 2021-2027 agregace", "Z170201000000", "INTERREG III A", "317-21711C",
                    "Vady panelové výstavby", "317411", "Podpora výstavby nájemních bytů a technické infrastruktury",
                    "217313", "INTERREG V-A Česká republika - Polsko", "INTER-11",
                    "INTERREG IIIA Česká republika - Slovenská republika", "CZ.04.4.84", "OP Technická pomoc",
                    "CZ.1.08", "Program přeshraniční spolupráce Česká republika - Svobodný stát Sasko 2014-2020",
                    "PPS-15", "OP Přeshraniční spolupráce Svobodný stát Bavorsko – Česká republika 2007-2013",
                    "CZ.3.19", "Podpora rozvoje hospodářsky slabých a strukturálně postižených regionů", "217113",
                    "Společný regionální operační program", "21711A", "INTERREG III A", "21711C",
                    "Program přeshraniční spolupráce Česká republika - Svobodný stát Bavorsko Cíl EÚS 2014-2020",
                    "PPS-14", "Technická infrastruktura", "117D06300", "Národní program PHARE 2003 -část II - GS",
                    "217512", "Podpora výstavby technické infrastruktury", "317-117513",
                    "Podpora výstavby nájemních bytů a technické infrastruktury", "317-217313",
                    "INTERREG V-A Slovensko - Česká republika", "INTER-12", "Podpora regenerace panelových sídlišť",
                    "217312", "Podpora vítězů soutěže Vesnice roku", "117D8210D", "Operační program Technická pomoc",
                    "OPTP 2021-2027 agregace", "Z170301000000",
                    "Podpora rozvoje hospodářsky slabých a strukturálně postižených regionů", "317-217113",
                    "INTERREG V-A Rakousko - Česká republika", "INTER-13", "Podpora oprav domovních olověných rozvodů",
                    "317-117515", "Phare", "717N01", "Podpora regenerace panelových sídlišť", "317-217312",
                    "Podpora rozvoje hospodářsky slabých a strukturálně postižených regionů", "217D11300",
                    "Podpora regenerace panelových sídlišt", "317-117512",
                    "Podpora revitalizace bývalých vojenských areálů", "117D81400",
                    "Podpora úprav bývalých vojenských areálů", "217D11800",
                    "Podpora regenerace brownfieldů pro nepodnikatelské využití", "117D08200",
                    "Podpora oprav domovních olověných rozvodů", "317-217316",
                    "Rekonstrukce a přestavba veřejných budov", "117D8220E",
                    "Podpora územně plánovacích dokumentací obcí", "117D55100",
                    "Jednotný programový dokument Praha - cíl 2", "21711B",
                    "Podpora územně plánovacích dokumentací obcí pro rok 2019 - 2023", "117D53100",
                    "Regenerace sídlišť", "117D06200", "Národní program PHARE 2003 -část II - GS", "317-217512",
                    "Podp. rozvoje prům. podnik. Subj. na území NUTS 2 SZ MS a dalších reg. se soustředěnou podp. státu",
                    "217116", "Podpora venkovské pospolitosti a spolupráce na rozvoji obcí", "117D8210K",
                    "Podpora úprav bývalých vojenských areálů", "317-217118",
                    "Jednotný programový dokument Praha - cíl 2", "317-21711B",
                    "Podpora obnovy a výstavby TI v bývalých vojenských újezdech Ralsko a Mladá", "217114",
                    "Podpora oprav domovních olověných rozvodů", "217316",
                    "Podpora vládou doporučených projektů v oblasti rozvoje regionů", "117D82500", "SAPARD", "SAPARD",
                    "Interreg IIIC", "CZ.04.4.87", "Interreg IIIB CADSES", "CZ.04.4.86", "Olověné rozvody", "117D06500",
                    "Infrastruktura a rekonstrukce", "217414", "Regenerace panelových sídlišť", "317412",
                    "EÚS 2021-2027 agregace", "Z170101000000", "NNV - NNO v oblasti regionálního rozvoje",
                    "Z170601000000",
                    "Podp. rozvoje prům. podnik. Subj. na území NUTS 2 SZ MS a dalších reg. se soustředěnou podp. státu",
                    "317-217116", "NNV - NNO v oblasti bydlení", "Z170602000000",
                    "Subkomponenta 4.1.3 Finanční podpora přípravy projektů souladných s cíli EU", "Z171702000000",
                    "Podpora bydlení 2013 - s nevyhlášeným staveb nebezpečí nebo nouzovým stavem", "117D02600",
                    "Architektonické a urbanistické soutěže obcí", "117D22100",
                    "Výstavba technické infrastruktury v oblastech se strategickou průmyslovou zónou", "117D16200",
                    "Stavby a pozemky HS ČR", "117D25100",
                    "Výstavba bytů v oblastech se strategickou průmyslovou zónou", "117D16100",
                    "Architektonické a urbanistické soutěže obcí", "117D15100",
                    "Podpora rekonstrukce bývalých vojenských objektů pro účely nájemního bydlení", "217412",
                    "Rozvoj a obnova majetku HS ČR", "117D13000",
                    "Tvorba studií a analýz možností využití vybraných brownfieldů", "117D08300",
                    "Aktualizace územně plánovací dokumentace obcí", "217413",
                    "Aktualizace územně plánovací dokumentace obcí", "317-217413",
                    "Podpora rozvoje severozápadních Čech a Moravskoslezského regionu", "217D11200",
                    "Reprodukce majetku", "117V01200", "Výstavba technické infrastruktury pro rozvoj města Kolín",
                    "317-217317",
                    "Pořízení a technická obnova investičního majetku ve správě Ministerstva pro místní rozvoj",
                    "317010", "Reprodukce majetku", "317-117012", "ESPON", "CZ.04.4.88", "FS Řídící orgán",
                    "2004/CZ/16/C/PA", "INTERACT", "CZ.04.4.89", "Podpora oprav vad panelové výstavby", "317-217315",
                    "OP – Praha – pól růstu", "117D09100",
                    "Podpora obnovy a výstavby TI v bývalých vojenských újezdech Ralsko a Mladá", "317-217114",
                    "Reprodukce majetku MMR", "217012", "SFPI - neinvestiční dotace agregace", "Z170501000000",
                    "Výstavba nájemních bytů a technické infrastruktury ve vlastnictví obcí", "317-317420",
                    "NNV - CZ PRES", "Z170604000000", "Podpora bydlení", "217310",
                    "Revitalizace brownfieldů pro jiné než hospodářské využití", "117D63100",
                    "Subkomponenta 4.1.4 Zefektivnění a posílení implementace Národního plánu obnovy", "Z171703000000"
                }
            },
            {
                CalculatedCategories.ZamestnanostTrhPrace, new[]
                {
                    "Operační program Zaměstnanost",
                    "Podpora podnikatelských subjektů zaměstnávajících občany se změněnou pracovní schopností",
                    "213410", "Podpora podnikatelských subjektů zaměstnávajících občany se ZPS", "313410",
                    "Služby v oblasti zaměstnanosti podpora poradensko-vzdělávacích středisek", "113D34D00",
                    "Operační program Rozvoj lidských zdrojů", "CZ.04.1.03", "EQUAL", "CZ.04.4.09",
                    "OP Lidské zdroje a zaměstnanost", "CZ.1.04",
                    "Posilování sociálního dialogu a budování kapacit sociálních partnerů", "CZ.1.04/1.1.01",
                    "Operační program Zaměstnanost+ agregace",
                    "Operační program Rozvoj lidských zdrojů", "CZ.04.1.03",
                    "Podpora infrastruktury pro rozvoj lidských zdrojů v průmyslu a podnikání - ŠKOLÍCÍ STŘEDISKA",
                    "322-222217", "Investice pro zajištění pracovních míst v utlumované části OKD", "322070",
                    "Rozvoj lidských zdrojů v průmyslu- SOP4", "322089",
                    "Podpora infrastruktury pro rozvoj lidských zdrojů v průmyslu a podnikání - ŠKOLÍCÍ STŘEDISKA",
                    "222217", "OP Lidské zdroje a zaměstnanost", "CZ.1.04",
                    "Podpora rozvoje pracovních příležitostí na území Ústeckého a Moravskoslezského kraje", "117D81600",
                    "Operační program Rozvoj lidských zdrojů", "CZ.04.1.03", "OP Lidské zdroje a zaměstnanost",
                    "CZ.1.04", "OP Z agregace", "Z334401000000",
                    "Operační program Zaměstnanost agregace", "Z151901000000",
                    "OP Lidské zdroje a zaměstnanost", "CZ.1.04",
                    "Operační program zaměstnanost", "135D12500"
                }
            },
            {
                CalculatedCategories.PrevenceKriminality, new[]
                {
                    "Podpora neinvestičních projektů v oblasti prevence kriminality", "314D08300",
                    "Pořízení obnova a provozování majetku v oblasti prevence kriminality", "114D05100",
                    "Pořízení a obnova majetku v oblasti prevence kriminality", "314D08200",
                    "Podpora reprodukce majetku v oblasti prevence kriminality", "314-214052",
                    "Pořízení a obnova majetku v oblasti prevence kriminality", "114D08200",
                    "Podpora reprodukce majetku v oblasti prevence kriminality", "214052",
                    "Pořízení obnova a provozování majetku v oblasti prevence kriminality", "314-114051",
                    "Prevence kriminality na místní úrovni - obce agregace", "Z142701000000",
                    "Materiálně technické zabezpečení prevence kriminality", "314013",
                    "Prevence sociálně patologických jevů", "Z140901000000",
                    "Rozvoj služeb pro oběti trestné činnosti poskytovaných na základě zákona č. 45/2013 Sb.",
                    "Z360401000000", "Rozvoj probačních a resocializačních programů pro dospělé pachatele agregace",
                    "Z360201000000", "Rozvoj probačních a resocializačních programů pro mladistvé delikventy agregace",
                    "Z360301000000", "Prevence korupčního jednání II agregace", "Z360501000000",
                    "Prevence korupčního jednání I agregace", "Z360601000000"
                }
            },
            {
                CalculatedCategories.RegionalniRozvoj, new[]
                {
                    "Jednotný programový dokument pro Cíl 3 regionu hl.m.Praha", "CZ.04.3.07",
                    "Integrovaný operační program", "CZ.1.06", "Národní plán obnovy agregace", "Z130701000000",
                    "Národní plán obnovy - neinvestiční výdaje II", "Z130703000000", "Podpora rozvoje-LEADER",
                    "329-229222", "Podpora rozvoje-LEADER", "229222", "SAPARD", "Regenerace veřejného prostranství",
                    "Integrovaný regionální operační program", "Facilita na podporu oživení a odolnosti", "129D50300",
                    "Integrovaný operační program", "CZ.1.06", "Odstranění následků povodní roku 2006", "329-229114",
                    "Výstavba a technická obnova vodovodů a úpraven vod", "229030",
                    "Odstranění následků povodní roku 2010", "229D11700",
                    "Podpora opatření k odstranění povodňových škod z roku 2002", "229113",
                    "Odstranění následků povodní roku 2013", "129D27200",
                    "Výstavba a technická obnova vodovodů a úpraven vod obcí do 5 tis. obyvatel", "329031",
                    "Podpora výstavby a obnovy kanalizací pro veřejnou potřebu", "129D18300",
                    "Podpora výstavby a technického zhodnocení infrastruktury kanalizací pro veřejnou potřebu",
                    "129D25300", "Podpora výstavby a obnovy vodovodů pro veřejnou potřebu", "129D18200",
                    "Výstavba a obnova vodovodů úpraven vod a souvisejících objektů", "329-229312",
                    "Odstranění následků povodní roku 2009", "229D11600",
                    "Podpora výstavby a technického zhodnocení infrastruktury vodovodů pro veřejnou potřebu",
                    "129D25200", "Podpora výstavby a technického zhodnocení kanalizací pro veřejnou potřebu II",
                    "129D30300", "Výstavba a technická obnova vodovodů a úpraven vod", "329030",
                    "Povodně 97-vodní toky", "329184",
                    "Výstavba a technická obnova kanalizací a čistíren odpadních vod", "229040",
                    "Povodně 97-vodní toky", "329181",
                    "Státní pomoc při obnově území postiženého povodní 2002 poskytovaná MZe", "229810",
                    "Podpora výstavby a technického zhodnocení vodovodů pro veřejnou potřebu II", "129D30200",
                    "Výstavba a obnova čistíren vod kanalizací a souvisejících objektů", "229D31300",
                    "Výstavba a obnova čistíren vod kanalizací a souvisejících objektů", "329-229313",
                    "Podpora opatření k odstranění povodňových škod z roku 2000", "229112",
                    "Odstranění následků povodní roku 2006", "229D11400",
                    "Podpora výstavby a technického zhodnocení infrastrukturykanalizací III", "129D41300",
                    "Podpora odstranění následků povodní roku 2006", "229114",
                    "Výstavba a obnova infrastruktury vodovodů", "229312",
                    "Podpora odstraňování povodňových škod způsobených povodněmi 2013", "129D14400",
                    "Podpora protipovodňových opatření podél vodních toků", "129D26500",
                    "Odstranění škod způsobených povodní 1997 - MZe", "329180",
                    "Výstavba a technická obnova kanalizací a čistíren odpadních vod", "329040",
                    "Výstavba a obnova infrastruktury kanalizací", "229313",
                    "Podpora výstavby a technického zhodnocení infrastruktury vodovodů III", "129D41200",
                    "Podpora odstraňování povodňových škod způsobených povodněmi 2009", "129D14200",
                    "Výstavba a obnova vodovodů úpraven vod a souvisejících objektů", "229D31200",
                    "Podpora odstraňování povodňových škod na infrastruktuře vodovodů", "329-229039",
                    "Odstranění následků povodní roku 2020", "129D37200",
                    "Vlachovice - vypořádání práv k nemovitým věcem dotčeným plánovanou realizací vodního díla",
                    "129D33200", "Odstranění následků povodní roku 2007", "229D11500",
                    "Výstavba a technická obnova vodovodů a úpraven vod", "329-229030",
                    "Výstavba a technická obnova vodovodů a úpraven vod v oblastech postižených povodní", "329183",
                    "Podpora odstraňování povodňových škod na infrastruktuře kanalizací", "329-229049",
                    "Podpora odstraňování povodňových škod způsobených povodněmi 2010", "129D14300",
                    "Výstavba a technická obnova kanalizací a čistíren odpadních vod", "329-229040",
                    "Odstraňování povodňových škod na hrázích a objektech rybníků a vodních nádrží", "129D13300",
                    "Podpora vodního hospodářství agregace", "Z290401000000", "Podpora projektových dokumentací",
                    "129D36300", "Podpora informačního procesu plánování v oblasti vod", "329-129151",
                    "Výstavba a technická obnova kanalizace v oblastech postižených povodní", "329049",
                    "Výstavba a technická obnova vodovodů a úpraven vod v oblastech postižených povodní", "329039",
                    "Podpora přípravy a realizace vyvolaných investic a staveb souvisejících s výstavbou vodního díla Nové Heřminovy",
                    "129D36600", "Podpora odkupu a scelování infrastruktury vodovodů a kanalizací", "129D42200",
                    "Podpora projektové přípravy významných akcí PPO", "129D50200",
                    "Podpora realizace vyvolaných investic souvisejících s výstavbou VD Nové Heřminovy", "129D50500",
                    "Přivaděče VD Kryry – nádrž Vidhostice; Kolešovický a Rakovnický potok – vypořádání práv k nemovitým věcem dotčeným plánovanou realizací přivaděčů",
                    "129D34400",
                    "Program Pomoc po tornádu agregace", "Z222701000000",
                    "MPO - povodňové akce nesplňující kritéria EIB", "322189",
                    "Výstavba a technická obnova inženýrských sítí průmyslových zón", "322050",
                    "Akreditace průmyslových zón", "322054", "Příprava průmyslových zón", "322051",
                    "Příprava a rozvoj průmyslových zón", "222D23200",
                    "Regenerace a podnikatelské využití brownfieldů I", "122D21100",
                    "Příprava a rozvoj podnikatelských parků", "222232", "Příprava a rozvoj podnikatelských parků",
                    "322-222232", "2.8. Regenerace brownfieldů pro podnikatelské využití (komponenta NPO)",
                    "Z220313000000", "Smart Parks for the Future", "122D20100", "Regenerace průmyslových zón", "322052",
                    "Integrovaný operační program", "CZ.1.06", "Příprava a rozvoj podnikatelských parků", "222V23200",
                    "Czechinvest * Interreg Danube * projekt PilotInnCities agregace", "Z225201000000",
                    "Program meziregionální spolupráce 2021+ (GDT)", "Z224901000000",
                    "Program nadnárodní spolupráce Central Europe 2021+", "Z225102000000",
                    "Zefektivnění a optimalizace procesu vyhodnocování sběru a správy dat potřebných pro mapování regionů a vytvoření Mapy podnikatelského prostředí",
                    "Z223803000000",
                    "Obnova obecního a krajského majetku postiženého živelní nebo jinou pohromou", "217D11700",
                    "Obnova obecního a krajského majetku postiženého živelní nebo jinou pohromou", "317-217117",
                    "Obnova obecního a krajského majetku po živelních pohromách v roce 2013", "117D91400",
                    "Obnova obecního a krajského majetku postiženého živelní nebo jinou pohromou", "217117",
                    "Podpora obnovy venkova po povodni 2002", "217816",
                    "Podpora při zajišť. dočas. náhr. ubyt. a dalších souv. potřeb v důsledku povodně nebo živ. pohromy",
                    "117D51700",
                    "Podpora rozvoje hospodářsky slabých a strukturálně postižených regionů po povodni 2002", "217818",
                    "Ukrajina – změny dokončených staveb stavební úpravy budov", "117D74100",
                    "Obnova obecního a krajského majetku po živelních pohromách v roce 2016", "117D91700",
                    "Obnova obecního a krajského majetku po živelních pohromách v roce 2012", "117D91300",
                    "Obnova obecního a krajského majetku po živelních pohromách v roce 2014", "117D91500",
                    "Obnova obecního a krajského majetku po živelních pohromách v roce 2018 a 2019", "117D91900",
                    "Obnova obecního a krajského majetku po živelních pohromách v roce 2017", "117D91800",
                    "Obnova obecního a krajského majetku po živelních pohromách v roce 2011", "117D91200",
                    "Obnova obecního a krajského majetku po živelních pohromách 2021", "117D92300",
                    "Podpora obnovy místních komunikací po povodni 2002", "217815",
                    "Podpora obnovy venkova po povodni 2002", "317-217816",
                    "Podpora rozvoje hospodářsky slabých a strukturálně postižených regionů po povodni 2002",
                    "317-217818", "Podpora obnovy bytového fondu po povodni 2002", "217817",
                    "Podpora výstavby obecních nájemních bytů pro občany postižené živelní pohromou", "117D51600",
                    "Obnova obecního a krajského majetku po živelních pohromách 2020", "117D92200",
                    "Obnova obecního a krajského majetku po živelních pohromách v roce 2015", "117D91600",
                    "Finanční pomoc záplavami postižených obcí při zajištění dočasného ubytování v r.1997", "317182",
                    "Podpora při zajištění náhrad. bydlení občanů postižených povodněmi", "317180",
                    "Podp.akt.územně plánovací dok. obcí postižených povodněmi 2002 se zaměřením na protipodňovou ochranu",
                    "217819",
                    "Odstranění škod způsobených povodněmi v roce 1997", "333081",
                    "Obnova majetku státních škol a výchovných zařízení poškozeného povodní 2002", "233118",
                    "Podpora obnovy majetku veřejných vysokých škol poškozeného povodní 2002", "233348",
                    "Odstranění škod způsobených povodněmi v roce 1998", "333082",
                    "Podpora obnovy majetku ČVUT v Praze poškozeného povodní 2002", "233328",
                    "Národní program přípravy ČR na členství v EU", "315070", "Integrovaný regionální operační program",
                    "Program na podporu projektů nestátních neziskových organizací agregace", "Z150601000000",
                    "Dotace resortním příspěvkových organizacím agregace", "Z152501000000",
                    "Komponenta 4.1 NPO - Systémová podpora veřejných investic", "Z152206000000",
                    "Program přeshraniční spolupráce INTERREG V-A ČR - Pl 2014+ agregace", "Z151601000000",
                    "Program přeshraniční spolupráce INTERREG V-A ČR - Ss 2014+ agregace", "Z152401000000",
                    "Státní pomoc při obnově území postiženého povodní 2006", "234218",
                    "Výdaje spojené se zajištěním plnění úkolů na území kraje a hlavního města Prahy", "Z143001000000",
                    "Rozvoj a obnova materiálně-technické základny dopravních opravárenských a stravovacích služeb",
                    "114V04400",
                    "Reprodukce investičního majetku dopravních opravárenských stravovacích rekreačních zařízení a služeb",
                    "314032", "Výstavba sídel veřejné správy - 2 etapa", "214412",
                    "Rozvoj a obnova mat tech základny dopravních opravárenských a stravovacích služeb", "214044",
                    "Rozvoj a obnova materiálně-technické základny bytového a ubytovacího fondu", "114V04600",
                    "Rozvoj a obnova mat tech základny správy a výstavby bytového fondu", "214046",
                    "Zajištění zasedání Mezinárodního měnového fondu a skupiny světové banky", "314230",
                    "Rozvoj a obnova materiálně - technické základny dopravních opravárenských a stravovacích služeb",
                    "114V51400",
                    "Rozvoj dobrovolnické služby agregace", "Z140701000000",
                    "Integrace cizinců na lokální úrovni agregace", "Z140601000000",
                    "Příspěvek obci na úhradu nákladů obci v souvislosti s azylovým zařízením", "Z141501000000",
                    "Integrace cizinců 2022 agregace", "Z140501000000",
                    "OP Azylový migrační a integrační fond agregace", "Z141601000000",
                    "Příspěvek obci na úhradu nákladů v souvislosti s azylovým zařízením", "Z141301000000",
                    "Státní integrační program", "Z141201000000",
                    "Program podpory fungování Regionálních dobrovolnických center", "Z143201000000",
                    "Adaptačně-integrační kurzy 2022 agregace", "Z140201000000",
                    "Dotační program pro NNO provozující evropské krizové či asistenční linky", "Z141001000000",
                    "NP Azylový migrační a integrační fond agregace", "Z142601000000", "Reforma azylových institucí",
                    "314074",
                    "Integrovaný regionální operační program", "Integrovaný operační program", "CZ.1.06",
                    "Podpora výstavby a obnovy komunální infrastruktury", "298D22300", "Dotace NF z Fondu soudržnosti",
                    "2004/CZ/16/C/PA/NF", "Dotace NF z Fondu solidarity", "NRES 2012/2002",
                    "Prostředky pro řešení aktuálních problémů ÚSC", "Z980101000000",
                    "Jednotný programový dokument pro Cíl 3 regionu hl.m.Praha", "CZ.04.3.07"
                }
            },
            {
                CalculatedCategories.SocialniSluzby, new[]
                {
                    "Program nedefinován 213311", "313-213311",
                    "Dotace pro přímo řízené organizace MPSV jinde nezařazené agregace", "Z131801000000",
                    "Podpora zavádění standardů kvality sociálních služeb", "213313",
                    "Podpora reprodukce majetku ústavů sociální péče a služeb", "213314",
                    "Pořízení a tech. obnova invest. majetku ve správě Ministerstva práce a sociálních věcí", "313010",
                    "Pořízení a tech. obnova invest. majetku ve správě ústavů sociální péče", "313040",
                    "Pořízení a tech. obnova invest. majetku ve správě ústavů sociální péče-náhrada restit. objektů",
                    "313041", "Pořízení a tech. obnova invest. majetku ve správě ústavů sociální péče", "313042",
                    "Pořízení a tech. obnova invest. majetku charitativních organizací a občanských sdružení", "313070",
                    "Podpora mobility soc. služeb", "013D31200",
                    "Podpora reprodukce movitého a nemovitého majetku soc. služeb", "013D31300",
                    "Reprodukce majetku center sociálních služeb", "013V33200",
                    "Podpora reprodukce majetku služeb sociální prevence a podpora mobility", "113D31200",
                    "Podpora reprodukce majetku služeb sociální péče", "113D31300", "Podpora výstavby domovů důchodců",
                    "113D31400",
                    "Podpora reprodukce majetku v odvětví práce a sociálních věcí podle návrhů Poslanecké sněmovny PČR",
                    "113D31500", "Služby v oblasti sociální integrace", "113D34B00",
                    "Zvýšení kvality a dostupnosti terénních ambulantních a pobytových sociálních služeb pro seniory",
                    "113D35200", "Reprodukce majetku ústavů sociální péče MPSV", "113V33200",
                    "Podpora reprodukce majetku služeb sociální prevence a podpora mobility", "313-113312",
                    "Podpora reprodukce majetku služeb sociální péče", "313-113313", "Podpora výstavby domovů důchodců",
                    "313-113314", "Podpora zavádění standardů kvality sociálních služeb", "313-213313",
                    "Podpora reprodukce majetku ústavů sociální péče a služeb", "313-213314",
                    "Kontinuální výzva pro oblast podpory 3.1. - služby v oblasti sociální integrace (c)",
                    "CZ.1.06/3.1.02",
                    "Dotační řízení MPSV pro kraje a Hlavní město Prahu v oblasti poskytování sociálních služeb agregace",
                    "Z130101000000",
                    "Dotační řízení MPSV v oblasti poskytování sociálních služeb s nadregionální či celostátní působností agregace",
                    "Z130201000000",
                    "Podpora veřejně účelných aktivit seniorských a proseniorských organizací s celostátní působností agregace",
                    "Z130301000000", "Podpora sociálních služeb agregace", "Z130401000000",
                    "Poskytování příspěvku na výkon sociální práce agregace", "Z130501000000",
                    "Neinvestiční transfer krajům a Hl. městu Praze na výplatu státního příspěvku pro zřizovatele pro děti vyžadující okamžitou pomoc agregace",
                    "Z131001000000", "Obec přátelská rodině a seniorům agregace - ostatní transfery", "Z131102000000",
                    "Národní dotační titul Rodina agregace", "Z131201000000",
                    "Mimořádné dotace pro poskytovatele sociálních služeb agregace", "Z132001000000",
                    "Demolice budov v sociálně vyloučených lokalitách", "117D08100",
                    "Podpora bydlení 2013 - krizový stav", "117D02500", "Podporované byty", "117D06400",
                    "Podpora výstavby podporovaných bytů", "217314",
                    "Výstavba a technická obnova domů s pečovatelskou službou", "317530",
                    "Odstraňování bariér v budovách domů s pečovatelskou službou a v budovách městských a obecních úřadů",
                    "117D61200", "Podpora výstavby podporovaných bytů", "317-117514",
                    "Podpora výstavby podporovaných bytů", "317-217314",
                    "Odstraňování bariér v budovách domů s pečovatelskou službou a v budovách městských a obecních úřadů",
                    "117D62200", "Podpora dostupnosti služeb", "117D8210I",
                    "NNV - NNO v oblasti bezbariérového užívání staveb", "Z170603000000", "Euroklíč", "117D62300",
                    "Euroklíč", "117D61300", "Výstavba sociálního a dostupného bydlení", "117D14100",
                    "Zajištění národního rozvojového programu mobility pro všechny", "233D01500", "133D32200",
                    "333-233015", "233015", "333125", "Pořízení a obnova pomůcek a zařízení pro tělesně postižené",
                    "333125", "NRPM - (Zajištění Národního rozvojového programu mobility pro všechny)", "133D35200",
                    "Program vyrovnání příležitostí pro občany se zdravotním postižením agregace", "Z350901000000",
                    "Národní plán vyrovnávání příležitostí pro občany se zdravotním postižením", "235322",
                    "Národní plán vyrovnávání příležitostí pro občany se zdravotním postižením", "235V32200",
                    "Národní plán vyrovnávání příležitostí pro občany se zdravotním postižením", "235D32200",
                    "Národní plán vyrovnávání příležitostí pro občany se zdravotním postižením", "335-235322",
                    "Státní podpora občanských sdružení", "335130",
                    "Vyrovnávání příležitostí pro občany se zdravotním postižením", "135D07300",
                    "Vyrovnávání příležitostí pro občany se zdravotním postižením", "335D71100",
                    "Lidská práva začleňování Romů a domácí a genderově podmíněné násilí agregace", "Z120301000000",
                    "Program podpory práce s rodinami osob odsouzených k výkonu trestu odnětí svobody agregace",
                    "Z360101000000"
                }
            },
            {
                CalculatedCategories.Sport, new[]
                {
                    "Podpora budování a obnovy míst aktivního a pasivního odpočinku", "117D8210H",
                    "Podpora obnovy sportovní infrastruktury", "117D8210B", "Podpora obnovy sportovní infrastruktury",
                    "117D8220B", "Podpora sportovně rekreační činnosti", "317325",
                    "Podpora budování a obnovy míst aktivního odpočinku", "117D8220H",
                    "Podpora materiálně technické základny sportovních organizací", "133D51200",
                    "Podpora rozvoje a obnovy mat.tech.základny sportovních organizací", "233512",
                    "Podpora rozvoje a obnovy mat.tech.základny sportovních organizací", "333-233512",
                    "Podpora materiálně technické základny sportu – ÚSC SK a TJ", "133D53100",
                    "Podpora rozvoje a obnovy mat.tech.základny sportovních organizací", "233D51200",
                    "Podpora rozvoje materiálně technické základny sportovní reprezentace", "133D51300",
                    "Výstavba a obnova závodních drah hřišť stadionů a přírodních areálů", "333512",
                    "Výstavba a obnova tělocvičen a sportovních hal", "333513",
                    "Podpora rozvoje a obnovy mat.tech.základny sportovní reprezentace", "233513",
                    "Rozvoj materiálně technické základny sportovních svazů pro potřeby reprezentace a talentované mládež",
                    "133D52300", "Podpora rozvoje a obnovy mat.tech.základny sportovní reprezentace", "333-233513",
                    "Podpora rozvoje a obnovy mat.tech.základny sportovní reprezentace", "233D51300",
                    "Výstavba a obnova zařízení a hřišť pro rekreační sport", "333511",
                    "Výzva SG na podporu přípravy sportovních talentů ve školách s oborem vzdělání Gymnázium se sportovní přípravou agregace",
                    "Z333601000000", "Výstavba a obnova zimních stadionů", "333515",
                    "Výstavba a obnova specielních sportovních objektů a zařízení", "333517",
                    "Podpora roz. a obnovy mat. tech. základny pro pořádání Mistrovství světa v klas.lyžování v roce 2009",
                    "333-233514", "Výstavba a obnova jiných než státních sportovních center", "333522",
                    "Výstavba a obnova sportovních zařízení", "333510",
                    "Výstavba a obnova sportovních a tělovýchovných zařízení", "333213",
                    "Reprodukce investičního majetku určeného k zabezpečení sportovní reprezentace", "333523",
                    "Podpora roz. a obnovy mat. tech. základny pro pořádání Mistrovství světa v klas.lyžování v roce 2009",
                    "233514", "Výstavba a obnova plaveckých zařízení a bazénů", "333514",
                    "Výstavba a obnova a úpravy sportovních zařízení pro zdravotně postižené", "333518",
                    "Výzva na podporu pohybových aktivit v MŠ ZŠ ŠD a ŠK", "Z333808000000",
                    "Výstavba a obnova sportovních a tělovýchovných zařízení", "333113",
                    "Výstavba a obnova sportovních a tělovýchovných zařízení", "333313", "Významné sportovní akce",
                    "133D52400", "Sportovní infrastruktura", "133D62200",
                    "Reprodukce investičního majetku organizací v sektoru tělovýchovy a mládeže", "333023",
                    "Národní sportovní centra", "133D52100"
                }
            },
            {
                CalculatedCategories.VzdelavaniSkolstvi, new[]
                {
                    "Centra odborné přípravy", "129D71200",
                    "Pořízení a tech. obnova invest. majetku v zařízeních učňovského školství v působnosti MZe",
                    "329020",
                    "Další profesní vzdělávání zaměstnanců podnikatelských subjektů v oblasti průmyslu - EDUCA",
                    "CZ.1.04/1.1.04",
                    "CzechInvest Akademie (CIA): rozšiřování odborných kapacit zaměstnanců agentury CzechInvest",
                    "Z223802000000", "ČVUT - návratná finanční výpomoc agregace", "Z220701000000",
                    "Operační program Výzkum vývoj a vzdělávání", "OP Vzdělávání pro konkurenceschopnost", "CZ.1.07",
                    "JAK ZED agregace", "Z332701000000", "Národní plán obnovy - komponenta 3.2 RgŠ agregace",
                    "Z332801000000", "Dotace na činnost škol a školských zařízení - církevní zřizovatel agregace",
                    "Z330501000000", "Výzva Adaptační skupiny pro děti cizince migrující z Ukrajiny agregace",
                    "Z332601000000", "Podpora rozvoje a obnovy mat.tech.základny ZŠ ve vlastnictví státu", "233112",
                    "Dotace na činnost škol a školských zařízení zřizovaných ÚSC a DSO agregace", "Z330101000000",
                    "Podpora soutěží a přehlídek", "Z333801000000",
                    "Rozvoj výukových kapacit mateřských a základních škol zřizovaných územně samosprávnými celky",
                    "133D31100", "Rozvoj materiálně technické základny mimoškolních aktivit dětí a mládeže",
                    "133D71100", "Podpora rozvoje a obnovy mat.tech.základny ZŠ ve vlastnictví státu", "233V11200",
                    "Výstavba a obnova kapacit pro vzdělávací a tvůrčí činnost", "333311",
                    "Podpora NNO v oblasti práce s dětmi a mládeží", "Z333803000000", "ZED_OP VVV agregace",
                    "Z330601000000", "Dotace", "Z334102000000",
                    "Výstavba a obnova kapacit pro vzdělávací a tvůrčí činnost", "333211",
                    "Podpora zajištění vybraných investičních podpůrných opatření při vzdělávání dětí žáků a studentů se",
                    "133D32100", "Dotace na činnost soukromých škol a školských zařízení agregace", "Z330201000000",
                    "Výstavba a obnova budov a staveb středních škol", "333210",
                    "Výzva na aktivity v oblasti primární prevence rizikového chování a podpory duševního zdraví ve školách a školských zařízeních agregace",
                    "Z333401000000", "Příspěvek", "Z334101000000",
                    "Obnova přístrojového a strojního vybavení vysokých škol", "333328",
                    "Podpora reprodukce majetku ČVUT v Praze", "233322", "Podpora nadaných žáků ZŠ a SŠ",
                    "Z333804000000", "Výzva na podporu vzdělávání v jazycích národnostních menšin agregace",
                    "Z333001000000", "Podpora koncepčního vzdělávacího rozvoje středních škol", "333220",
                    "Výstavba a obnova budov a staveb vysokých škol", "333310",
                    "Podpora studia zahraničních stipendistů na veřejných vysokých školách", "Z333702000000",
                    "Výzva Jazykové kurzy pro děti cizince migrující z Ukrajiny agregace", "Z332501000000",
                    "Podpora koncepčního vzdělávacího rozvoje základních škol", "333120",
                    "Výstavba a obnova budov a staveb základních škol", "333110",
                    "Podpora rozvoje a obnovy mat.tech.základny UK v Praze", "233D31200",
                    "Podpora rozvoje a obnovy mat.tech.základny UK v Praze", "333-233312",
                    "Rozvoj a obnova materiálně technické základny Českého vysokého učení technického v Praze",
                    "133D21D00", "Specifický vysokoškolský výzkum agregace", "Z330801000000",
                    "Výstavba a obnova kapacit pro vzdělávací a tvůrčí činnost", "333111",
                    "Rozvoj a obnova materiálně technické základny Univerzity Karlovy v Praze", "133D21E00",
                    "Výzva na podporu romských žáků a studentů středních škol konzervatoří a vyšších odborných škol agregace",
                    "Z333501000000", "Národní plán obnovy - VVŠ agregace", "Z334701000000",
                    "Výzva na podporu integrace romské menšiny agregace", "Z332901000000",
                    "Náhrada objektů středních škol vydaných v restituci podle zákona č.164/1998 Sb.", "333216",
                    "Pořízení a obnova učebních pomůcek", "333123",
                    "Rozvoj a obnova materiálně technické základny Akademie múzických umění v Praze", "133D21A00",
                    "Rozvoj a obnova materiálně technické základny Vysoké školy ekonomické v Praze", "133D21F00",
                    "UNIS", "Z333802000000", "Inovace studijních programů", "333325",
                    "Náhrada objektů základních škol vydaných v restituci podle zák.č.164/1998 Sb.", "333116",
                    "Inovace a rozvoj laboratoří ateliérů a pracovišť pro praktickou výuku", "333327",
                    "Reprodukce majetku organizací zřízených MŠMT", "233V01400",
                    "Rozvoj a obnova mater. tech. základny škol a škol. zař. zřiz. MŠMT v sys. náhradní výchovné péče",
                    "133V11200", "Podpora rozvoje a obnovy mat.tech.základny UK v Praze", "233312",
                    "Podpora projektů uskutečněných na základě česko-rakouské spolupráce v oblasti terciálního vzdělávání",
                    "Z333701000000", "Podpora reprodukce majetku ČVUT v Praze", "233D32200",
                    "Výstavba a obnova ubytovacích a stravovacích kapacit", "333314",
                    "Podpora rozvoje a obnovy mat.tech.základny ČZU v Praze", "23334C",
                    "Podpora reprodukce invest.majetku zařízení pro mimoškolní aktivity dětí a mládeže", "333413",
                    "Výstavba a obnova ubytovacích a stravovacích kapacit", "333212",
                    "Reprodukce investičního majetku organizací v sektoru základních a středních škol", "333021",
                    "Rozvoj a obnova materiálně technické základny VVŠ - ubytovací a stravovací kapacity - akce",
                    "133D22100", "Reprodukce majetku organizací zřízených MŠMT", "233014",
                    "Rozvoj a obnova materiálně technické základny Vysoké školy báňské-Technické univerzity Ostrava",
                    "133D21O00", "ERC CZ agregace", "Z332101000000", "Na učitelích záleží agregace", "Z333101000000",
                    "Náhrada objektů vysokých škol vydaných v restituci podle zákona č.164/1998 Sb.", "333317",
                    "Rozvoj a obnova materiálně technické základny Vysoké školy chemicko-technologické v Praze",
                    "133D21G00", "Podpora rozvoje a obnovy mat.tech.základny MZLU v Brně", "23334P",
                    "Podpora reprodukce majetku ČVUT v Praze", "333-233322",
                    "Podpora rozvoje a obnovy mat.tech.základny MZLU v Brně", "233D34P00",
                    "Reprodukce investičního majetku organizací zřízených MŠMT", "333020",
                    "Výzva Prázdninové jazykové kurzy pro děti cizince migrující z Ukrajiny agregace", "Z334301000000",
                    "Podpora rozvoje a obnovy mat.tech.základny TU v Liberci", "23334I",
                    "Podpora rozvoje a obnovy mat.tech.základny TU v Ostravě", "23334X", "Podpora refundací cestovného",
                    "Z334601000000",
                    "Rozvoj a obnova materiálně technické základny Vysoké školy technické a ekonomické v Českých Budějov",
                    "133D21X00", "Podpora rozvoje a obnovy mat.tech.základny TU v Ostravě", "233D34X00",
                    "Podpora rozvoje a obnovy mat.tech.základny VŠCHT v Praze", "233D34B00",
                    "Podpora rozvoje a obnovy mat.tech.základny TU v Ostravě", "233D34X00",
                    "Podpora rozvoje a obnovy mat.tech.základny VŠCHT v Praze", "233D34B00",
                    "Podpora rozvoje dvojjazyčného vzdělávání na středních školách v České republice", "Z334001000000",
                    "Rozvoj materiálně technické základny mimoškolních aktivit dětí a mládeže", "133D72100",
                    "Podpora koncepčního vzdělávacího rozvoje vysokých škol", "333320",
                    "Podpora rozvoje VŠ polytechnické v Jihlavě", "233D34Z00",
                    "Rekonstrukce a modernizace stávajících objektů MU v Brně", "233333",
                    "Rozvoj a obnova materiálně technické základny Mendelovy univerzity v Brně", "133D21K00",
                    "Výstavba a obnova ubytovacích a stravovacích kapacit", "333112",
                    "Podpora rozvoje a obnovy mat.tech.základny VUT v Brně", "233D34O00",
                    "Rozvoj a obnova materiálně technické základny Akademie výtvarných umění v Praze", "133D21B00",
                    "Podpora rozvoje a obnovy mat.tech.základny UJEP v Ústí nad Labem", "23334H",
                    "Rozvoj a obnova materiálně technické základny Jihočeské univerzity v Českých Budějovicích",
                    "133D21Y00", "Rozvoj a obnova materiálně technické základny Slezské univerzity v Opavě",
                    "133D21P00", "Obnova zaříz. a dovyb.obč.sdruž", "333414",
                    "Rozvoj a obnova materiálně technické základny Akademie múzických umění v Praze", "133D22A00",
                    "Podpora obnovy majetku UK v Praze poškozeného povodní 2002", "233318",
                    "Reprodukce majetku sektoru státní správy MŠMT", "333-233012",
                    "Rozvoj a obnova materiálně technické základny Západočeské univerzity v Plzni", "133D21Z00",
                    "Podpora rozvoje a obnovy mat.tech.základny JAMU v Brně", "23334S",
                    "Inovace a rozvoj laboratoří ateliérů a pracovišť pro praktickou výuku", "333222",
                    "Rekonstrukce a modernizace stávajících objektů MU v Brně", "333-233333",
                    "Rozvoj a obnova materiálně technické základny Janáčkovy akademie múzických umění v Brně",
                    "133D21I00", "Výzva na podporu aktivit integrace cizinců v regionálním školství agregace",
                    "Z333301000000", "Podpora rozvoje a obnovy mat.tech.základny UP v Olomouci", "23334T",
                    "Podpora rozvoje a obnovy mat.tech.základny UTB ve Zlíně", "23334U",
                    "Podpora rozvoje a obnovy mat.tech.základny VŠCHT v Praze", "23334B",
                    "Podpora rozvoje a obnovy mat.tech.základny ČZU v Praze", "233D34C00",
                    "Podpora účasti dětí na předškolním vzdělávání agregace", "Z334901000000",
                    "Rozvoj a obnova materiálně technické základny Ostravské univerzity v Ostravě", "133D21N00",
                    "Rozvoj a obnova materiálně technické základny Univerzita Hradec Králové", "133D24100",
                    "Rozvoj a obnova materiálně technické základny Univerzity Pardubice", "133D21U00",
                    "Rozvoj a obnova materiálně technické základny České zemědělské univerzity v Praze", "133D21C00",
                    "Evropská jazyková cena LABEL agregace", "Z334801000000", "Letní škola slovanských studií",
                    "Z333703000000", "Podpora rozvoje a obnovy mat.tech.základny AMU v Praze", "23334D",
                    "Podpora rozvoje a obnovy mat.tech.základny JU v Českých Budějovicích", "23334L",
                    "Rozvoj a obnova materiálně technické základny Vysoká škola ekonomická v Praze", "133D22F00",
                    "Výstavba a obnova ubytovacích a stravovacích zařízení", "333516",
                    "Podpora rozvoje a obnovy mat.tech.základny TU v Liberci", "233D34I00",
                    "Podpora rozvoje a obnovy mat.tech.základny VUT v Brně", "23334O",
                    "Podpora vybraných projektů rozvoje výukových kapacit základního vzdělávání zřizovaného obcemi a dobr",
                    "133D33100", "Přístrojové a technické vybavení", "233D31300",
                    "Rozvoj a obnova materiálně technické základny Masarykovy univerzity v Brně", "133D21J00",
                    "Rozvoj a obnova materiálně technické základny Mendelova univerzita v Brně", "133D22K00",
                    "Podpora rozvoje a obnovy mat.tech.základny JU v Českých Budějovicích", "233D34L00",
                    "Podpora rozvoje a obnovy mat.tech.základny UJEP v Ústí nad Labem", "233D34H00",
                    "Podpora rozvoje a obnovy mat.tech.základny Univerzity v Pardubicích", "23334K",
                    "Podpora rozvoje a obnovy mat.tech.základny VŠE v Praze", "233D34A00",
                    "Podpora rozvoje a obnovy mat.tech.základny VŠUP v Praze", "23334F",
                    "Podpora rozvoje a obnovy mat.tech.základny ČZU v Praze", "333-23334C",
                    "Pořízení a obnova dopravních prostředků", "333224",
                    "Rozvoj a obnova mater. tech. základny škol a škol. zař. zřiz. MŠMT v oblasti speciálního vzdělávání",
                    "133V11100",
                    "Rozvoj a obnova materiálně technické základny Univerzity J.E.Purkyně v Ústí nad Labem",
                    "133D21W00", "Rozvoj a obnova materiálně technické základny Univerzity Palackého v Olomouci",
                    "133D21Q00", "Reprodukce majetku sektoru státní správy MŠMT", "233012",
                    "Kurzy českého jazyka pro studenty a pedagogické pracovníky z Ukrajiny agregace",
                    "Kurzy českého jazyka pro studenty a pedagogické pracovníky z Ukrajiny agregace", "Z334501000000",
                    "Podpora rozvoje a obnovy mat.tech.základny SU v Opavě", "23334Y",
                    "Podpora rozvoje a obnovy mat.tech.základny UTB ve Zlíně", "233D34U00",
                    "Podpora rozvoje a obnovy mat.tech.základny VFU v Brně", "233D34R00",
                    "Podpora rozvoje a obnovy mat.tech.základny VŠCHT v Praze", "333-23334B",
                    "Podpora rozvoje a obnovy mat.tech.základny VŠE v Praze", "23334A",
                    "Rozvoj a obnova materiálně technické základny Univerzita Karlova v Praze", "133D22E00",
                    "Rozvoj a obnova materiálně technické základny České vysoké učení technické v Praze", "133D22D00",
                    "Podpora rozvoje a obnovy mat.tech.základny Univerzity v Hradci Králové", "23334J",
                    "Podpora rozvoje a obnovy mat.tech.základny ZU v Plzni", "23334M",
                    "Dotace na podporu dalších úkolů v oblasti VŠ agregace", "Z334201000000",
                    "Podpora rozvoje a obnovy mat.tech.základny AVU v Praze", "23334E",
                    "Podpora rozvoje a obnovy mat.tech.základny Ostravské univerzity", "233D34V00",
                    "Podpora rozvoje a obnovy mat.tech.základny VFU v Brně", "333-23334R",
                    "Rozvoj a obnova materiálně technické základny Univerzity Hradec Králové", "133D21T00",
                    "Rozvoj a obnova materiálně technické základny Veterinární a farmaceutická univerzita Brno",
                    "133D22L00",
                    "Rozvoj a obnova materiálně technické základny pedagogických fakult veřejných vysokých škol",
                    "133D24200", "Podpora rozvoje a obnovy mat.tech.základny VFU v Brně", "23334R",
                    "Rozvoj a obnova materiálně technické základny Janáčkova akademie múzických umění v Brně",
                    "133D22I00", "Rozvoj a obnova materiálně technické základny Česká zemědělská univerzita v Praze",
                    "133D22C00", "Podpora rozvoje VŠ polytechnické v Jihlavě", "333-23334Z",
                    "Podpora rozvoje a obnovy mat.tech.základny MZLU v Brně", "333-23334P",
                    "Podpora rozvoje a obnovy mat.tech.základny Ostravské univerzity", "23334V",
                    "Podpora rozvoje a obnovy mat.tech.základny UP v Olomouci", "233D34T00",
                    "Podpora rozvoje a obnovy mat.tech.základny ZU v Plzni", "233D34M00",
                    "Podpora rozvoje výukových kapacit základního vzdělání zřizovaného obcemi a dobrovolnými svazky obcí",
                    "133D34200", "Reprodukce investičního majetku organizací v sektoru vysokých škol", "333022",
                    "Rozvoj a obnova materiálně technické základny Technické univerzity v Liberci", "133D21S00",
                    "Rozvoj a obnova materiálně technické základny Vysoké školy polytechnické Jihlava", "133D21V00",
                    "Rozvoj a obnova materiálně technické základny Vysoké školy umělecko-průmyslové v Praze",
                    "133D21H00", "Reprodukce majetku pedagogických center", "233013",
                    "Rozvoj a obnova materiálně technické základny Vysoké učení technické v Brně", "133D22M00",
                    "Rozvoj vzdělávání učitelů", "333322",
                    "Výzva na podporu školního stravování žáků základních škol agregace", "Z333901000000",
                    "Podpora rozvoje a obnovy mat.tech.základny AMU v Praze", "233D34D00",
                    "Podpora rozvoje a obnovy mat.tech.základny AVU v Praze", "233D34E00",
                    "Podpora rozvoje a obnovy mat.tech.základny AVU v Praze", "333-23334E",
                    "Podpora rozvoje a obnovy mat.tech.základny SU v Opavě", "233D34Y00",
                    "Podpora rozvoje a obnovy mat.tech.základny TU v Ostravě", "333-23334X",
                    "Podpora rozvoje a obnovy mat.tech.základny UJEP v Ústí nad Labem", "333-23334H",
                    "Podpora vybraných projektů rozvoje výukových kapacit základního vzdělávání zřizovaného obcemi a dobrovolnými svazky obcí",
                    "133D34100", "Přístrojové a technické vybavení", "233313",
                    "Rozvoj a obnova materiálně technické základny Univerzity Tomáše Bati ve Zlíně", "133D21R00",
                    "Rozvoj a obnova materiálně technické základny Veterinární a farmaceutické univerzity v Brně",
                    "133D21L00",
                    "Rozvoj a obnova materiálně technické základny Vysoká škola chemicko-technologická v Praze",
                    "133D22G00", "Výstavba a obnova budov pro řízení a administrativu", "333312",
                    "Rozvoj a obnova materiálně technické základny Masarykova univerzita", "133D22J00",
                    "Rozvoj a obnova materiálně technické základny Slezská univerzita v Opavě", "133D22P00",
                    "Rozvoj a obnova materiálně technické základny Univerzita Jana Evangelisty Purkyně v Ústí nad Labem",
                    "133D22W00", "Systémový rozvoj česko-německé spolupráce v oblasti vzdělávání a mládeže",
                    "Z333805000000", "Výstavba univerzitního kampusu MU v Brně Bohunicích", "233332",
                    "Jazyk jako brána: Soustavné jazykové vzdělávání pro Ukrajince – migranty směřující k jejich terciálnímu vzdělávání v českém jazyce",
                    "Z334502000000", "Národní plán obnovy - komponenta 3.2 VVŠ agregace", "Z332802000000",
                    "Podpora rozvoje a obnovy mat.tech.základny JAMU v Brně", "333-23334S",
                    "Podpora rozvoje a obnovy mat.tech.základny JU v Českých Budějovicích", "333-23334L",
                    "Podpora rozvoje a obnovy mat.tech.základny TU v Liberci", "333-23334I",
                    "Podpora rozvoje a obnovy mat.tech.základny UTB ve Zlíně", "333-23334U",
                    "Podpora rozvoje a obnovy mat.tech.základny Univerzity v Hradci Králové", "233D34J00",
                    "Podpora rozvoje a obnovy mat.tech.základny Univerzity v Pardubicích", "233D34K00",
                    "Podpora rozvoje a obnovy mat.tech.základny VŠUP v Praze", "233D34F00",
                    "Podpora rozvoje a obnovy mat.tech.základny VŠUP v Praze", "333-23334F",
                    "Podpora rozvoje a obnovy mat.tech.základny ZŠ ve vlastnictví státu", "333-233112",
                    "Pořízení a obnova dopravních prostředků", "333124", "Přístrojové a technické vybavení",
                    "333-233313", "Rozvoj a obnova mat.tech.základny STK", "23301B",
                    "Rozvoj a obnova materiálně technické základny Jihočeská univerzita v Českých Budějovicích",
                    "133D22Y00", "Rozvoj a obnova materiálně technické základny Ostravská univerzita", "133D22N00",
                    "Rozvoj a obnova materiálně technické základny Technická univerzita v Liberci", "133D22S00",
                    "Rozvoj a obnova materiálně technické základny Vysoká škola uměleckoprůmyslová v Praze",
                    "133D22H00", "Rozvoj a obnova materiálně technické základny Vysokého učení technického v Brně",
                    "133D21M00", "Vzdělávací infrastruktura", "133D62100",
                    "Výstavba univerzitního kampusu MU v Brně Bohunicích", "233D33200",
                    "Obnova objektů bezúpl.převedených z Fondu dětí a mládeže na občan.sdružení", "333411",
                    "Podpora rozvoje regionálních informačních center mládeže", "333412",
                    "Reprodukce investičního majetku ve správě školských úřadů", "333012",
                    "Rozvoj a obnova materiálně technické základny Akademie výtvarných umění v Praze", "133D22B00",
                    "Rozvoj a obnova materiálně technické základny Univerzita Hradec Králové", "133D22T00",
                    "Rozvoj a obnova materiálně technické základny Univerzita Palackého v Olomouci", "133D22Q00",
                    "Rozvoj a obnova materiálně technické základny Univerzita Pardubice", "133D22U00",
                    "Rozvoj a obnova materiálně technické základny Vysoká škola báňská - Technická univerzita Ostrava",
                    "133D22O00", "Rozvoj a obnova materiálně technické základny Vysoká škola polytechnická Jihlava",
                    "133D22V00",
                    "Rozvoj a obnova materiálně technické základny Vysoká škola technická a ekonomická v Českých Budějovicích",
                    "133D22X00", "Rozvoj materielně tech. základny mimoškolních aktivit dětí a mládeže", "333410",
                    "Rozvoj provozní praxe studentů bakalářského studia", "333323",
                    "Vzdělávací rozvoj státních škol a výchovných zařízení", "233114",
                    "Zajištění administrace programu Vzdělávání v rámci Finančního mechanismu EHP 2014-2021 agregace",
                    "Z330401000000",
                    "Zajištění implementace Finančního mechanismu EHP 2014-2021 pro program Vzdělávání agregace",
                    "Z330301000000",
                    "Rozvoj a obnova mat tech základny školství", "214V02200",
                    "Rozvoj a obnova materiálně-technické základny školství", "114V02200",
                    "Rozvoj a obnova mat tech základny pro vzdělávací a nakladatelskou činnost", "214023",
                    "Rozvoj a obnova mat tech základny pro vzdělávací a nakladatelskou činnost", "214V02300",
                    "Rozvoj a obnova materiálně-technické základny pro vzdělávací a nakladatelskou činnost", "314035",
                    "OP Vzdělávání pro konkurenceschopnost", "CZ.1.07",
                    "Specializační vzdělávání - univerzity agregace", "Z351501000000",
                    "Rozvoj a obnova mat.tech.základny školských a vzdělávacích zařízení", "235V02300",
                    "Rozvoj a obnova mat.tech.základny školských a vzdělávacích zařízení", "235023",
                    "Podpora rozvoje a obnovy materiálně-technické základny regionálního školství v působnosti obcí",
                    "298D22800",
                    "Podpora obnovy a rozvoje materiálně technické základny regionálního školství v působnosti obcí",
                    "298D23200",
                    "Podpora rozvoje a obnovy materiálně technické základny regionálních škol v okolí velkých měst",
                    "298D21300"
                }
            },
            {
                CalculatedCategories.PodporaDoZahranici, new[]
                {
                    "Zpracování kulturního dědictví a aktuálních témat s Německem Rakouskem a Lichtenštejnskem agregace",
                    "Z060401000000",
                    "Realizace bilaterální ZRS", "Z060607000000",
                    "Priority zahraniční politiky ČR a mezinárodní vztahy agregace", "Z060501000000",
                    "Komplexní a stabilizační pomoc", "Z060103000000", "GRV a osvěta veřejnosti", "Z060603000000",
                    "Česko-polské fórum", "Z060502000000", "Posilování kapacit realizátorů ZRS", "Z060604000000",
                    "Podpora trilaterální spolupráce", "Z060605000000", "Rozvojove-ekonomické partnerství",
                    "Z060606000000", "Spolupráce s krajanskými komunitami v zahraničí agregace", "Z060201000000",
                    "Kofinancování - Program transformační spolupráce", "Z060304000000",
                    "Spolupráce s krajanskými komunitami OVD", "Z060202000000", "Humanitární pomoc - program BVA",
                    "Z060105000000", "Zahraniční rozvojová spolupráce agregace", "Z060601000000",
                    "Veřejné VŠ v ZRS zemích", "Z060602000000"
                }
            },
            {
                CalculatedCategories.Zdravotnictvi, new[]
                {
                    "Rozvoj a obnova mat tech základny lázeňské rehabilitační péče", "214043",
                    "Reprodukce investičního majetku lázeňských a rehabilitačních zařízení", "314031",
                    "Specializační vzdělávání lékaři - dotace na celé specializační vzdělávání", "Z351101000000",
                    "Specializační vzdělávání lékaři - dotace na specializační vzdělávání v základním kmeni",
                    "Z351102000000", "Specializační vzdělávání nelékaři agregace", "Z351301000000",
                    "Vybavení nemocnic poliklinik a záchranné služby stroji a zařízeními", "335020",
                    "Výstavba a technická obnova nemocnic a léčebných zařízení v působnosti MZ", "335010",
                    "Národní program kolorektálního karcinomu", "235313",
                    "Časově uzavřená výzva pro oblast podpory 6.3.2.", "CZ.1.06/3.2.01",
                    "Podpora reprodukce majetku regionálních zdravotnických zařízení", "335210",
                    "Podpora rozvoje a obnovy materiálně technické základny ZZS", "135D08200",
                    "Soubor projektů NPO podle usnesení vlády č.493/1993", "335120",
                    "Mimořádný dotační program pro poskytovatele lůžkové péče s cílem prevence negativních dopadů psychické a fyzické zátěže a obnovy psychických a fyzických sil pro pracovníky ve zdravotnictví v souvislosti s epidemií COVID-19 pro rok 2023 agregace",
                    "Z351701000000", "Podpora vybavení regionálního zdravotnictví stroji a zařízeními", "235D21200",
                    "Podpora rozvoje nemovitého majetku regionálního zdravotnictví", "235214",
                    "Program podpora zdraví zvyšování efektivity a kvality zdravotní péče agregace", "Z350601000000",
                    "Podpora rozvoje nemovitého majetku regionálního zdravotnictví", "235D21400",
                    "Podpora NNO pečujících o pacienty v terminálním stadiu agregace", "Z350701000000",
                    "Podpora rozvoje a obnovy mat.tech.základny záchranných služeb", "235D21300",
                    "Podpora snížení zátěže obyvatelstva ionizujícím zářením", "235316",
                    "Mimořádný dotační program na kompenzaci nákladů na nespotřebované léčivé přípravky obsahující monoklonální protilátky v souvislosti s onemocněním covid-19 agregace",
                    "Z352101000000", "Program protidrogové politiky", "235312",
                    "Krizová připravenost přímo řízených organizací agregace", "Z352001000000",
                    "Podpora vybavení regionálního zdravotnictví stroji a zařízeními", "235212",
                    "Podpora rozvoje a obnovy mat.tech.základny odborných léčebných ústavů", "235V13200",
                    "Podpora rozvoje a obnovy mat.tech.základny FTNsP Praha 4", "235V11D00",
                    "Národní program řešení problematiky HIV/AIDS agregace", "Z350801000000",
                    "ZZS dotace krajům - neinvestiční agregace", "Z351601000000",
                    "Podpora zubních lékařů v oblastech s omezenou dostupností zdravotních služeb agregace",
                    "Z350501000000", "Podpora hospicové paliativní péče stroji a zařízeními", "135D10200",
                    "Národní program HIV-AIDS", "235332",
                    "Podpora rozvoje a obnovy mat.tech.základny FN U sv. Anny Brno", "23511I",
                    "Podpora rozvoje a obnovy mat.tech.základny odborných léčebných ústavů", "235132",
                    "Podpora rozvoje a obnovy mat.tech.základny FN Brno", "235V11H00",
                    "Podpora rozvoje a obnovy mat.tech.záklany FNsP Ostrava", "235V11K00",
                    "Podpora rozvoje a obnovy mat.tech.základny záchranných služeb", "235213",
                    "Perinatologický program", "235314",
                    "Podpora vybavení regionálního zdravotnictví stroji a zařízeními", "335-235212",
                    "Rozvoj a obnova mat tech základny národních lékařských knihoven", "235022",
                    "Podpora rozvoje a obnovy mat.tech.základny FN Praha 5 Motol", "235V11E00",
                    "Podpora rozvoje nemovitého majetku regionálního zdravotnictví", "335-235214",
                    "Perinatologický program", "235V31400",
                    "Podpora rozvoje a obnovy mat.tech.základny FN Praha 8 - Bulovka", "235V11C00",
                    "Podpora rozvoje a obnovy mat.tech.základny FN U sv. Anny Brno", "235V11I00",
                    "Program Zdraví financovaný z EHP fondů 2014-2021", "Z350101000000",
                    "Podpora rozvoje a obnovy mat.tech.základny FNKV Praha", "235V11A00",
                    "Pořízení a tech. obnova invest. majetku Národní lékařské knihovny", "335140",
                    "Podpora rozvoje HS ČR a VZS", "235315", "Monitoring zdravotního stavu obyvatelstva ČR", "235334",
                    "Náhrada restituovaných objektů nemocnic poliklinik a léčebných zařízení", "335011",
                    "Podpora rozvoje a obnovy mat.tech.základny FN Brno", "23511H", "Hodnocení rizik", "235333",
                    "Podpora rozvoje a obnovy mat.tech.základny FN Olomouc", "235V11J00",
                    "Podpora rozvoje FN Hradec Králové", "235V11G00",
                    "Dotační program pro poskytovatele zdravotnické záchranné služby s cílem zajištění provozu jednotného informačního systému v letecké záchranné službě v České republice agregace",
                    "Z352701000000", "Podpora rozvoje a obnovy mat.tech.základny VFN Praha 2", "23511B",
                    "Podpora rozvoje krajských hygienických stanic", "235013", "Perinatologický program", "235D31400",
                    "Podpora rozvoje a obnovy mat.tech.základny VFN Praha 2", "235V11B00",
                    "Podpora rozvoje a obnovy mat.tech.základny záchranných služeb", "335-235213",
                    "Pořízení a tech. obnova invest. majetku organizací zřízených MZ", "335050",
                    "Podpora rozvoje a obnovy mat.tech.základny FTNsP Praha 4", "23511D",
                    "Podpora rozvoje a obnovy mat.tech.základny FN Plzeň", "235V11F00", "Program protidrogové politiky",
                    "335-235312", "Podpora rozvoje a obnovy mat.tech.základny FN Praha 8 - Bulovka", "23511C",
                    "Podpora rozvoje a obnovy mat.tech.základny lázeňských léčebných ústavů", "235V13300",
                    "Perinatologický program", "335-235314", "Podpora rozvoje a obnovy mat.tech.záklany FNsP Ostrava",
                    "23511K", "Agregace komponenta 6.1 agregace", "Z352501000000",
                    "Podpora rozvoje a obnovy mat.tech.ostatních oborů", "235V12500",
                    "Podpora rozvoje a obnovy mat.tech.základny FN Praha 5 Motol", "23511E",
                    "Podpora rozvoje a obnovy mat.tech.základny gynekologie a neonatologie", "235V12300",
                    "Podpora rozvoje a obnovy mat.tech.základny odborných léčebných ústavů", "335-235132",
                    "Program protidrogové politiky", "235V31200", "Program spinálních jednotek", "235317",
                    "Soubor projektů ZPL", "335110", "Odstranění škod způsobených povodní 1997 - MZ", "335180",
                    "Podpora rozvoje a obnovy mat.tech.základny onkologie", "235V12200",
                    "Podpora rozvoje a obnovy materiálně technického vybavení pro řešení krizových situací",
                    "235V22200", "Podpora snížení zátěže obyvatelstva ionizujícím zářením", "335-235316",
                    "Rozvoj státní správy v působnosti MZ", "235012", "Národní program HIV-AIDS", "335-235332",
                    "Podpora rozvoje a obnovy mat.tech.ostatních oborů", "235125",
                    "Podpora rozvoje a obnovy mat.tech.základny FNKV Praha", "23511A",
                    "Podpora rozvoje a obnovy mat.tech.základny kardiologie", "235V12400",
                    "Podpora rozvoje a obnovy mat.tech.základny lázeňských léčebných ústavů", "235133",
                    "Podpora rozvoje a obnovy mat.tech.základny onkologie", "235122", "Hodnocení rizik", "335-235333",
                    "Podpora rozvoje a obnovy mat.tech.základny FN Plzeň", "23511F",
                    "Podpora rozvoje a obnovy mat.tech.základny gynekologie a neonatologie", "235123",
                    "Podpora rozvoje krajských hygienických stanic", "235V01300", "Program podpory hyperbaroxie",
                    "335-235318", "Program spinálních jednotek", "235V31700", "Program spinálních jednotek",
                    "335-235317", "3. akční program EU v oblasti zdraví 2014-2020 agregace", "Z352201000000",
                    "EU4Health agregace", "Z352601000000",
                    "Podpora rozvoje nemovité infrastruktury hospicové paliativní péče", "135D10300",
                    "Podpora rozvoje specializovaných pracovišť", "135D04200",
                    "Specializační vzdělávání lékaři - Podpora odborných periodik agregace", "Z351901000000",
                    "Zdraví agregace", "Z120501000000"
                }
            },
            {
                CalculatedCategories.Zemedelstvi, new[]
                {
                    "Podpora lesního hospodářství agregace", "Z290301000000",
                    "Údržba a obnova stávajících kulturních prvků venkovské krajiny", "129D66200", "OP Rybářství",
                    "CZ.1.25", "Společná zemědělská politika – PRV", "Z290101000000",
                    "OP Rozvoj venkova a multifunkční zemědělství", "CZ.04.1.04",
                    "Společná zemědělská politika – Přímé platby", "Z290102000000",
                    "Podpora obnovy a budování závlahového detailu a optimalizace závlahových sítí – II. etapa",
                    "129D31200", "Operační program Rybářství 2014-2020 agregace", "Z291301000000",
                    "Operační program Rybářství", "Operační program Rybářství",
                    "Podpora obnovy a budování závlahového detailu a optimalizace závlahových sítí", "129D16200",
                    "Podpora agrárního sektoru agregace", "Z291001000000",
                    "Operační program Rybářství 2021-2027 agregace", "Z290801000000", "Povodně 97-lesy", "329182",
                    "Údržba a oprava polních cest", "129D66600",
                    "Podpora agrárního sektoru – Zemědělské národní dotace - SZIF", "Z290902000000",
                    "Společná zemědělská politika – SOT", "Z290103000000",
                    "Podpora agrárního sektoru – Podpůrné a další programy", "Z290901000000", "Povodně 98-lesy",
                    "329192", "Výstavba a technická obnova lesního hopodářství", "329051",
                    "OP Rozvoj venkova a multifunkční zemědělství", "CZ.04.1.04"
                }
            },
            {
                CalculatedCategories.ZivotniProstredi, new string[]
                {
                    "Z130901000000", "Operační program Životní prostředí", "00020729", "62933591",
                    "Podpora opatření na rybnících a malých vodních nádržích ve vlastnictví obcí", "129D29300",
                    "Podpora opatření na drobných vodních tocích rybnících a malých vodních nádržích", "129D29200",
                    "Podpora opatření na rybnících a malých vodních nádržích ve vlastnictví obcí - 2. etapa",
                    "129D39300", "Podpora protipovodňových opatření podél vodních toků", "129D12300",
                    "Podpora opatření na drobných vodních tocích a malých vodních nádržích - 2. etapa", "129D39200",
                    "Obnova odbahnění a rekonstrukce rybníků a výstavba vodních nádrží", "129D13200",
                    "Podpora obnovy odbahnění a rekonstrukce rybníků a vodních nádrží", "229212",
                    "Zvyšování průtočné kapacity vodní toky", "229063", "Stanovování záplavových území", "229064",
                    "Podpora protipovodňových opatření podél vodních toků", "329-129123",
                    "Podpora vymezování záplavových území a studií odtokových poměrů", "329-129125",
                    "Podpora výstavby obnovy rekonstrukce a odbahnění rybníků a vodních nádrží", "129D28200",
                    "Výstavba a obnova poldrů nádrží a hrází", "229062",
                    "Obnova odbahnění a rekonstrukce rybníků a výstavba vodních nádrží", "329-129132",
                    "Podpora protipovodňových opatření s retencí", "129D26400",
                    "Podpora opatření pro zmírnění negativních dopadů sucha a nedostatku vody I.", "129D40300",
                    "Obnova rybníků a vodních nádrží po povodni 2002", "229218",
                    "Podpora protipovodňových opatření s retencí", "129D12200",
                    "Podpora obnovy odbahnění a rekonstrukce rybníků a vodních nádrží", "329-229212",
                    "Podpora projektové dokumentace pro stavební řízení", "129D26300",
                    "Podpora protipovodňových opatření podél vodních toků", "129D36500", "Studie odtokových poměrů",
                    "229065", "Vymezení rozsahu území ohrožených povodněmi", "229066",
                    "Podpora zvyšování bezpečnosti vodních děl", "129D12400",
                    "Podpora vymezování záplavových území a studií odtokových poměrů", "129D12500",
                    "Protipovodňová opatření", "329060",
                    "VD Kryry - vypořádání práv k nemovitým věcem dotčeným plánovanou realizací vodního díla",
                    "129D34200", "Podpora projektové dokumentace pro územní řízení", "129D26200",
                    "Podpora protipovodňových opatření s retencí", "129D36400", "Studie odtokových poměrů", "329064",
                    "Podpora zadržování vody v suchých nádržích na drobných vodních tocích", "129D12600",
                    "Podpora zpracování podkladů pro návrh plánů oblastí povodí", "329-129152",
                    "Senomaty a Šanov - vypořádání práv k nemovitým věcem dotčeným plánovanou realizací vodních děl",
                    "129D34300", "Odstranění havarijních situací na rybnících a vodních nádržích", "129D28300",
                    "Povodně 98-vodní toky", "329191",
                    "Podpora opatření na malých vodních nádržích a drobných vodních tocích – 3. etapa", "129D49200",
                    "Stanovování záplavových území", "329063",
                    "Odstranění havarijních stavů na rybnících a vodních nádržích", "129D13400",
                    "Podpora odtěžení nánosů z nádrží", "129D17300", "Podpora zvyšování bezpečnosti vodních děl",
                    "329-129124", "Odstranění škod způsobených povodní 1998 - zásobování pitnou vodou", "329193",
                    "Podpora projektové a inženýrské přípravy navržených opatření k řešení dopadů plánovaného rozšíření těžby polského hnědouhelného dolu Turów na české území",
                    "129D30400", "Výstavba a obnova poldrů nádrží a hrází", "329061",
                    "Zvyšování průtočné kapacity vodní toků", "329062",
                    "Podpora obnovy a zabezpečení vodárenských soustav", "129D40200",
                    "Podpora protipovodňových opatření s retencí", "329-129122",
                    "Podpora protipovodňových opatření podél a na vodních tocích", "129D50400",
                    "Podpora odstraňování povodňových škod na infrastruktuře kanalizací", "229049",
                    "Podpora projektové přípravy", "129D43200", "Podpora zvýšení funkčnosti a bezpečnosti vodních děl",
                    "129D17200", "OP Životní prostředí", "CZ.1.02",
                    "Podpora zahlazování následků hornické činnosti u státního podniku DIAMO", "122D11200",
                    "Odstraňování následků těžby uranu v oblasti Stráže pod Ralskem státním podnikem DIAMO",
                    "122D24100", "Cirkulární ekonomika a recyklace a průmyslová voda", "Z220306000000",
                    "Podpora zahlazování následků hornické činnosti u státního podniku DIAMO", "322-122112",
                    "Podpora zahlazování následků hornické činnosti u státního podniku Palivový kombinát Ústí",
                    "122D11300", "Sanace těžby uranu ve s.p. DIAMO Stráž pod Ralskem", "322020",
                    "Odstraňování následků těžby uranu v oblasti Stráže pod Ralskem státním podnikem DIAMO na léta 2023 - 2027",
                    "122D26100",
                    "Podpora zahlazování následků hornické činnosti u státního podniku Palivový kombinát Ústí n.L.",
                    "322-122113", "Sanace těžby uranu ve s.p.DIAMO Stráž pod Ralskem", "222112",
                    "Neinvestiční dotace na správu skládek s. p. Diamo agregace", "Z221101000000",
                    "Odstraňování následků těžby uranu v oblasti Stráže pod Ralskem státním podnikem DIAMO agregace",
                    "Z221201000000", "Rozvoj čisté mobility", "Z220305000000",
                    "Opatření k ochraně život.prostředí v podnikatelském sektoru v působnosti MPO", "322030", "Živel",
                    "117D73100",
                    "OP Infrastruktura - životní prostředí", "CZ.04.1.21", "FS: sektor ŽP", "2004/CZ/16/C/PE",
                    "333115", "333215", "Výstavba a obnova staveb zabezpečujících ochranu životního prostředí",
                    "333316",
                    "Nová zelená úsporám - Rodinné domy - 3. Výzva", "115D28103", "OP Životní prostředí", "CZ.1.02",
                    "Nová zelená úsporám - Rodinné domy", "115D28100", "Nová zelená úsporám - Rodinné domy - 2. Výzva",
                    "115D28102", "Nová zelená úsporám - Bytové domy - 2. Výzva", "115D28202",
                    "Podpora adaptace nelesních ekosystémů", "115D16500",
                    "Zkvalitnění nakládání s odpady a odstraňování SEZ", "Z150303000000",
                    "Revitalizace říčních systémů", "315050", "OP Infrastruktura - životní prostředí", "CZ.04.1.21",
                    "Podpora opatření v ochraně přírody", "315-215216", "Revitalizace retenční schopnosti krajiny",
                    "215115", "Drobné vodohospodářské ekologické akce", "315060", "Podpora adaptace vodních ekosystémů",
                    "115D16400", "Pořízení a obnova budov a staveb majetku MŽP", "115V01300",
                    "Zelená úsporám - budovy veřejného sektoru", "115D29100",
                    "Výstavba a obnova ČOV a kanalizace vč.umělých mokřadů komunální sféry", "215117",
                    "Adaptace nelesních ekosystémů na změnu klimatu", "115D17500",
                    "Podprogram A na podporu projektů NNO působících v oblasti ochrany životního prostředí a udržitelného rozvoje",
                    "Z150602000000", "Správa nezcizitelného státního majetku v ZCHÚ", "115V01200",
                    "Správa nezcizitelného majetku v ZCHÚ", "215012", "Pořízení a obnova strojů a zařízení MŽP",
                    "115V01400", "Pořízení obnova a provozování ICT resortu", "115V01100",
                    "Adaptace vodních ekosystémů na změnu klimatu", "115D17400", "Podpora péče o ZCHÚ, PO a EVL",
                    "115V16200", "MŽP Povodně 2013", "115D27200",
                    "Výstavba a obnova ČOV a kanalizace vč.umělých mokřadů komunální sféry", "315-215117",
                    "KP LIFE agregace", "Z151401000000",
                    "Státní podpora při obnově území postiženého povodní v roce 2006", "315-215119",
                    "Zlepšování kvality ovzduší a omezování emisí", "Z150304000000",
                    "Podpora adaptace lesních ekosystémů", "115D16600", "Řešení nestabilit svahů v ČR", "215124",
                    "Státní podpora při obnově území postiženého povodní v roce 2006", "215D11900",
                    "Zlepšování stavu přírody a krajiny", "Z150302000000", "Příspěvek zoologickým zahradám",
                    "Z150901000000", "Pořízení a obnova budov a staveb v majetku MŽP", "115V02300",
                    "Revitalizace retenční schopnosti krajiny", "315053",
                    "Pořízení a tech. obnova invest. majetku chráněných krajinných oblastí a národních parků", "315020",
                    "Informační podpora adaptačních opatření na extrémní hydromeorologické jevy - povodně a jakost vody",
                    "115V18100", "Operační program Rozvoj lidských zdrojů", "CZ.04.1.03",
                    "Pořízení a tech. obnova invest. majetku ve správě Ministerstva životního prostředí", "315010",
                    "Odborná podpora a monitoring", "115V16700", "Modernizace předpovědní a výstražné služby ČHMÚ",
                    "215130", "Revitalizace retenční schopnosti krajiny", "315-215115", "Řešení nestabilit svahů v ČR",
                    "315-215124", "Nová zelená úsporám - Náklady státu na administraci", "115D28400",
                    "Pořízení, obnova a provozování ICT resortu", "115V02100",
                    "Zlepšování kvality ovzduší a omezování emisí - OPST", "Z150801000000",
                    "Revitalizace přirozené funkce vodních toků s revitalizací retenční schopnosti krajiny", "215118",
                    "Revitalizace přirozené funkce vodních toků", "215112",
                    "Zakládání a revitalizace prvků ÚSES vázaných na vodní režim", "215113",
                    "Nová zelená úsporám - Bytové domy - 3. Výzva", "115D28203",
                    "Obnova majetku MŽP poškozeného povodní 2002", "215018",
                    "Pořízení a tech. obnova invest. majetku k řešení úkolů vědy a výzkumu", "315920",
                    "Upgrade imisního monitoringu a zpracování emisních a imisních dat", "115V19200",
                    "Podpora opatření v ochraně čistoty vod", "315-215213",
                    "Nová zelená úsporám - Adaptační a mitigační opatření", "115D28500", "MŽP Povodně 2010",
                    "115D27100",
                    "Revitalizace přirozené funkce vodních toků s revitalizací retenční schopnosti krajiny",
                    "315-215118", "Správa nezcizitelného státního majetku v ZCHU", "115V02200",
                    "Upgrade stanic pro sběr klimatických dat", "115V19300", "Odstraňování příčných překážek na tocích",
                    "215114", "Pořízení a technická obnova invest. majetku organizací vědy a výzkumu", "315910",
                    "Pořízení, obnova a provozování ICT systému řízení MŽP", "215011",
                    "Revitalizace přirozené funkce vodních toků", "315-215112",
                    "Adaptace lesních ekosystémů na změnu klimatu", "115D17600",
                    "Institucionální podpora výzkumných organizací agregace", "Z151301000000",
                    "Podpora opatření v ochraně čistoty vod", "215D21300",
                    "Rozvoj a obnova mat.tech.základny Správy NP Podyjí", "21501G",
                    "Podpora opatření v oblasti enviromentálního práva a legislativy", "215218",
                    "Rozvoj a obnova mat.tech.základny Správy Krkonošského NP", "21501E",
                    "Vodohospodářské projekty nad 100 mil. EUR + technická asistence FS", "215213",
                    "Odstranění škod způsobených povodní 1997 - MŽP", "315180",
                    "Pořízení a obnova strojů a zařízení MŽP", "115V02400", "Životní prostředí a infrastruktura",
                    "115D26200", "Program přeshraniční spolupráce INTERREG V-A ČR - Bv 2014+ agregace", "Z151701000000",
                    "Podpora adaptace lesních ekosystémů", "115V16600",
                    "Program přeshraniční spolupráce INTERREG V-A ČR - Rk 2014+ agregace", "Z151501000000",
                    "Zakládání a revitalizace prvků systému ekologické stability vazaných na vodní režim", "315052",
                    "Hlásný systém povodňové ochrany", "215122", "Nutná obnova přirozené funkce vodních toků", "215826",
                    "Rozvoj a obnova mat.tech.základny ČGS", "21501K", "Zlepšování kvality ovzduší a omezování emisí",
                    "Z150402000000", "Likvidace škod po živelních pohromách roku 2014", "115D27300",
                    "Nová zelená úsporám - Bytové domy - 1. Výzva", "115D28201",
                    "OP Nadnárodní spolupráce Central Europe 2014+ agregace", "Z152101000000",
                    "OP Vzdělávání pro konkurenceschopnost", "CZ.1.07", "Podpora adaptace vodních ekosystémů",
                    "115V16400", "Podprogram B na podporu koordinačních projektů v ochraně přírody a krajiny",
                    "Z150603000000", "Revitalizace přirozené funkce vodních toků", "315051",
                    "Podprogram C na podporu dlouhodobých systémových projektů v oblasti environmentálního vzdělávání, výchovy a osvěty, environmentálního poradenství a udržitelného rozvoje",
                    "Z150604000000", "Dotace Státnímu fondu životního prostředí ČR - OPST", "Z150803000000",
                    "Integrovaný operační program", "CZ.1.06", "Nová zelená úsporám - Budovy veřejného sektoru",
                    "115D28300", "Odstraňování příčných překážek na tocích", "315-215114",
                    "Prevence v územích ohrožených povodněmi, sesuvy a dalšími klimatickými vlivy", "315030",
                    "Revitalizace retenční schopnosti krajiny", "215D11500", "Rozvoj a obnova mat. tech. základny ČHMU",
                    "21501N", "Rozvoj a obnova mat.tech.základny Správy NP a CHKO Šumava", "21501F",
                    "Výstavba a obnova ČOV a kanalizace vč.umělých mokřadů komunální sféry", "215D11700",
                    "Zakládání a revitalizace prvků ÚSES vázaných na vodní režim", "315-215113",
                    "Komponenta 7.3 NPO - Komplexní reforma poradenství pro renovační vlnu", "Z152207000000",
                    "Rozvoj a obnova mat.tech.základny VÚ KOZ", "21501I", "Rozvoj a obnova mat.tech.základny VÚ TGM",
                    "21501J", "Vyhodnocení katastrofální povodně v srpnu roku 2002", "215125",
                    "Výstavba a obnova hlásných systémů", "315031", "Hlásný systém povodňové ochrany", "315-215122",
                    "Modernizace předpovědní a výstražné služby ČHMÚ", "315-215130",
                    "Národní plán obnovy - Komponenty 2.2 a 2.9 agregace", "Z150501000000",
                    "Obnova migrační prostupnosti a ekologické stability krajiny", "215825",
                    "Obnova ČOV a kanalizací v obcích do 2000 EO poškozených povodní 2002", "215812",
                    "Odstraňování příčných překážek na tocích", "215D11400", "Podpora opatření v ochraně ovzduší",
                    "315-215212", "Podpora opatření v ochraně přírody", "215D21600",
                    "Pořízení dokumentace záplavových území", "215123", "Pořízení dokumentace záplavových území",
                    "315-215123", "Výstavba a obnova rybích přechodů", "315056",
                    "Výstavba nových kořenových čistíren (KČOV) zakládání umělých mokřadů", "315055",
                    "Zajištění projektů rezortních organizací MŽP", "115D02500",
                    "Zprac. kon. strukt.přírodě blízkých protipovod. a protieroz.opa.v prior.pov.podle Plánu hlav.povodí",
                    "315-215116", "Komplexní monitoring hydrosféry - Fond soudržnosti", "215219",
                    "Komponenta 2.4 NPO - Rozvoj čisté mobility", "Z152202000000",
                    "Komponenta 2.5 NPO - Renovace budov a ochrana ovzduší", "Z152203000000",
                    "Komponenta 2.6 NPO - Ochrana přírody a adaptace na změnu klimatu", "Z152208000000",
                    "Komponenta 2.7 NPO - Cirkulární ekonomika recyklace a průmyslová voda", "Z152204000000",
                    "Nová zelená úsporám - Národní plán obnovy agregace", "Z150201000000",
                    "Podpora financování vodohospodářských projektů", "115D33200"
                }
            },
            { CalculatedCategories.FondHejtmana, new string[] { } },
            { CalculatedCategories.FondPrimatora, new string[] { } }
        };
        

        private static IEnumerable<Category> _textToCalculatedCategory(
            Dictionary<CalculatedCategories, string[]> keywords, string categoryName)
        {
            categoryName = categoryName.ToLower();
            //List<Category> res = new();
            /*
            foreach (var cat in keywords)
            {
                if (cat.Value.Any(m => m.Contains(categoryName, StringComparison.OrdinalIgnoreCase) || categoryName.Contains(m, StringComparison.OrdinalIgnoreCase)))
                {
                    if (res.Any(m => m.TypeValue == (int)cat.Key) == false)
                        res.Add(new Category { TypeValue = (int)cat.Key, Created = DateTime.Now, Probability = 1 });
                }

            }
            return res;
            */
            var res = keywords
                .Where(m => m.Value.Contains(categoryName, StringComparer.OrdinalIgnoreCase))
                .Select(m => new Category { TypeValue = (int)m.Key, Created = DateTime.Now, Probability = 1 });
            return res;
        }

        public static IEnumerable<Category> CategoryNameToCalculatedCategory(string categoryName)
        {
            return _textToCalculatedCategory(CategoryNameDictionary, categoryName);
        }


        public static IEnumerable<Category> ToCalculatedCategory(Subsidy item)
        {
            if (item == null)
                return Array.Empty<Category>();
            List<Category> cats = new();
            if (!string.IsNullOrEmpty(item.Category))
                cats.AddRange(CategoryNameToCalculatedCategory(item.Category) ?? Array.Empty<Category>());

            if (!cats.Any() && DataValidators.CheckCZICO(item.SubsidyProviderIco))
                cats.AddRange(_textToCalculatedCategory(CategoryNameDictionary, item.SubsidyProviderIco));

            if (!cats.Any() && !string.IsNullOrEmpty(item.ProgramCode) && item.ProgramCode?.Length > 3)
                cats.AddRange(_textToCalculatedCategory(CategoryNameDictionary, item.ProgramCode));

            if (!cats.Any() && !string.IsNullOrEmpty(item.ProgramCode) && item.ProgramCode?.Length > 3)
                cats.AddRange(_textToCalculatedCategory(ProgramsDictionary, item.ProgramCode));

            if (!cats.Any() && !string.IsNullOrEmpty(item.ProgramName))
                cats.AddRange(_textToCalculatedCategory(ProgramsDictionary, item.ProgramName));

            return cats;
        }

        public void SetCategories(IEnumerable<Category> cats)
        {
            if (cats?.Count() > 0)
            {
                Category1 = cats.OrderByDescending(o => o.Probability).First();
                if (cats.Count() > 1)
                    Category2 = cats.OrderByDescending(o => o.Probability).Skip(1).First();
                if (cats?.Count() > 2)
                    Category3 = cats.OrderByDescending(o => o.Probability).Skip(2).First();
            }
            else
            {
                Category1 = null;
                Category2 = null;
                Category3 = null;
            }
        }

        public class Category
        {
            [Number]
            public int TypeValue { get; set; }

            [Number]
            public decimal Probability { get; set; }

            [Date]
            public DateTime Created { get; set; }


            public CalculatedCategories CalculatedCategory()
            {
                return (CalculatedCategories)TypeValue;
            }

            public string CalculatedCategoryDescription()
            {
                return CalculatedCategory().ToNiceDisplayName();
            }
        }
        
        #endregion
    }
}