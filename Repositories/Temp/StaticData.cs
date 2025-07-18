﻿using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.OrgStrukturyStatu;
using HlidacStatu.Extensions;
using HlidacStatu.Repositories.Analysis;
using HlidacStatu.Repositories.Statistics;
using Microsoft.EntityFrameworkCore; //using HlidacStatu.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Serilog;
using static HlidacStatu.Entities.Osoba.Statistics;

namespace HlidacStatu.Repositories
{

    public static class StaticData
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(StaticData));

        [Serializable]
        public class FirmaName
        {
            public string Jmeno { get; set; }
            public string Koncovka { get; set; }
        }

        static object lockObj = new object();
        static bool initialized = false;


        public static string Dumps_Path = null;
        public static string[] Mestske_Firmy = new string[] { };


        public static Devmasters.Cache.AWS_S3.AutoUpdatebleCache<OrganizacniStrukturyUradu> OrganizacniStrukturyUraduCache = null;
        public static DateTime OrganizacniStrukturyUraduExportDate;

        public static Devmasters.Cache.AWS_S3.Cache<IEnumerable<AnalysisCalculation.IcoSmlouvaMinMax>> FirmyCasovePodezreleZalozene = null;
        public static Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_aktualni_Cache = null;
        public static Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_nedavne_Cache = null;
        public static Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_vsechny_Cache = null;

        public static Devmasters.Cache.AWS_S3.Cache<Tuple<Osoba.Statistics.RegistrSmluv, Entities.Insolvence.RizeniStatistic[]>[]> Insolvence_firem_politiku_Cache = null;



        public static Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaUradyStat> UradyObchodujiciSFirmami_s_vazbouNaPolitiky_aktualni_Cache = null;
        public static Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaUradyStat> UradyObchodujiciSFirmami_s_vazbouNaPolitiky_nedavne_Cache = null;
        public static Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaUradyStat> UradyObchodujiciSFirmami_s_vazbouNaPolitiky_vsechny_Cache = null;

        public static Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaUradyStat> UradyObchodujiciSNespolehlivymiPlatciDPH_Cache = null;
        public static Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaUradyStat> NespolehlivyPlatciDPH_obchodySurady_Cache = null;

        public static Devmasters.Cache.LocalMemory.AutoUpdatedCache<List<Sponzoring>> SponzorujiciFirmy_Vsechny = null;
        public static Devmasters.Cache.LocalMemory.AutoUpdatedCache<List<Sponzoring>> SponzorujiciFirmy_Nedavne = null;

        public static Devmasters.Cache.LocalMemory.Cache<List<double>> BasicStatisticData = null;

        public static Devmasters.Cache.AWS_S3.Cache<string> CzechDictCache =
            new Devmasters.Cache.AWS_S3.Cache<string>(
                new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
            TimeSpan.Zero, "Czech.3-2-5.dic.txt", (obj) =>
            {
                string s = Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/Czech.3-2-5.dic.txt").Result;
                return s;
            }, null);
        public static Devmasters.Cache.AWS_S3.Cache<string> CrawlerUserAgentsCache = new Devmasters.Cache.AWS_S3.Cache<string>(new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
            TimeSpan.Zero, "crawler-user-agents.json", (obj) =>
            {
                return Devmasters.Net.HttpClient.Simple.GetAsync("https://somedata.hlidacstatu.cz/appdata/crawler-user-agents.json").Result;
            }, null);



        public static Devmasters.Cache.AWS_S3.Cache<Dictionary<int, Osoba.Statistics.VerySimple[]>> TopPoliticiObchodSeStatem =
            new Devmasters.Cache.AWS_S3.Cache<Dictionary<int, Osoba.Statistics.VerySimple[]>>(new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") }, Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"), Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"), Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
    TimeSpan.Zero, "TopPoliticiObchodSeStatem", (obj) =>
    {
        List<Osoba.Statistics.RegistrSmluv> allStats = new();
        Devmasters.Batch.Manager.DoActionForAll(OsobaRepo.Politici.Get(),
            (o) =>
            {
                if (o != null)
                {
                    var stat = o.StatistikaRegistrSmluv(Relation.AktualnostType.Nedavny);
                    if (stat.SmlouvyStat_SoukromeFirmySummary().HasStatistics && stat.SmlouvyStat_SoukromeFirmySummary().Summary().PocetSmluv > 0)
                    {
                        allStats.Add(stat);
                    }

                }
                return new Devmasters.Batch.ActionOutputData();
            },
            Util.Consts.outputWriter.OutputWriter,
            Util.Consts.progressWriter.ProgressWriter,
            true, //!System.Diagnostics.Debugger.IsAttached,
            maxDegreeOfParallelism: 6, prefix: "TopPoliticiObchodSeStatem loading ", monitor: new MonitoredTaskRepo.ForBatch());


        Dictionary<int, Osoba.Statistics.VerySimple[]> res = new();
        res.Add(0,
                allStats.OrderByDescending(o => o.SmlouvyStat_SoukromeFirmySummary().Summary().CelkovaHodnotaSmluv).Take(100)
                .Union(allStats.OrderByDescending(o => o.SmlouvyStat_SoukromeFirmySummary().Summary().PocetSmluv).Take(100))
                .Select(m => new VerySimple()
                {
                    OsobaNameId = m.OsobaNameId,
                    CelkovaHodnotaSmluv = m.SmlouvyStat_SoukromeFirmySummary().Summary().CelkovaHodnotaSmluv,
                    PocetSmluv = m.SmlouvyStat_SoukromeFirmySummary().Summary().PocetSmluv,
                    Year = 0,
                }
                )
                .ToArray()
            );
        foreach (int year in HlidacStatu.Entities.KIndex.Consts.ToCalculationYears)
        {
            res.Add(year,
                    allStats.OrderByDescending(o => o.SmlouvyStat_SoukromeFirmySummary()[year].CelkovaHodnotaSmluv).Take(100)
                    .Union(allStats.OrderByDescending(o => o.SmlouvyStat_SoukromeFirmySummary()[year].PocetSmluv).Take(100))
                    .Select(m => new VerySimple()
                    {
                        OsobaNameId = m.OsobaNameId,
                        CelkovaHodnotaSmluv = m.SmlouvyStat_SoukromeFirmySummary()[year].CelkovaHodnotaSmluv,
                        PocetSmluv = m.SmlouvyStat_SoukromeFirmySummary()[year].PocetSmluv,
                        Year = year,
                    }
                    )
                    .ToArray()
                );

        }

        return res;
    }, null);


        public static Dictionary<string, TemplatedQuery> Afery = new Dictionary<string, TemplatedQuery>();

        public static Devmasters.Cache.LocalMemory.Cache<Dictionary<string, NespolehlivyPlatceDPH>> NespolehlivyPlatciDPH = null;

        //public static SingletonManagerWithSetup<Data.External.TwitterPublisher, Tweetinvi.Models.TwitterCredentials> TweetingManager = null;

        public static Devmasters.Cache.LocalMemory.Cache<Darujme.Stats> DarujmeStats = null;

        public static Devmasters.Cache.LocalMemory.Cache<Dictionary<string, string>> ZkratkyStran_cache = null;


        public static string[] HejtmaniOd2016 = new string[] {
            "jaroslava-jermanova",
            "ivana-straska",
            "josef-bernard",
            "jana-vildumetzova",
            "oldrich-bubenicek",
            "martin-puta",
            "jiri-stepan",
            "martin-netolicky",
            "jiri-behounek",
            "bohumil-simek",
            "ladislav-oklestek",
            "jiri-cunek",
            "ivo-vondrak",
            "adriana-krnacova",
            };
        public static string[] HejtmaniOd2020 = new string[] {
            "jiri-behounek","josef-bernard","oldrich-bubenicek","jiri-cunek","zdenek-hrib","jaroslava-pokorna-jermanova","petr-kubis","martin-netolicky","ladislav-oklestek","martin-puta","bohumil-simek","jiri-stepan","ivana-straska","ivo-vondrak"
        };
        public static string[] Poslanci2017Novacci = new string[] {
"adam-vojtech-1","alena-gajduskova","antonin-stanek","dan-tok","dana-balcarova-3","daniel-pawlas","dominik-feri","frantisek-elfmark-1",
"frantisek-kopriva-20","ilona-mauritzova","ivan-bartos","ivan-jac-4","ivana-nevludova","ivo-vondrak-2","jakub-michalek","jan-bauer",
"jan-cizinsky","jan-hrncir","jan-kobza-1","jan-kubik-9","jan-lipavsky","jan-posvar-3","jan-rehounek","jana-krutakova","jana-levova-7",
"jana-vildumetzova","jaroslav-dvorak-110","jaroslav-martinu","jiri-blaha-82","jiri-hlavaty","jiri-masek-50","jiri-strycek",
"jiri-ventruba","josef-belica-2","julius-spicak","karel-krejza","karla-marikova","karla-slechtova","katerina-valachova",
"klara-dostalova","lenka-kozlova-4","lubomir-volny-1","lucie--safrankova","lukas-barton","lukas-cernohorsky","lukas-kolarik-1",
"marek-vyborny","marian-bojko","martin-baxa","martin-jiranek-1","mikulas-ferjencik-1","mikulas-peksa","milan-hnilicka-4",
"miloslav-rozner","monika-jarosova-6","monika-oborna","milan-pour","olga-richterova","ondrej-profant","ondrej-vesely","patrik-nacher",
"pavel-juricek-4","pavel-ruzicka-19","pavel-stanek-1","pavel-zacek-18","petr-beitl-1","petr-dolinek","petr-sadovsky-4",
"petr-tresnak-12","radek-holomcik","radek-koten","radek-rozvoral","radek-zlesak","radovan-vich-1","robert-pelikan",
"stanislav-blaha","stanislav-fridrich-2","stanislav-juranek","tatana-mala-2","tereza-hythova","tomas-hanzel","tomas-martinek-10",
"tomas-vymazal-7","vaclav-klaus-2","veronika-vrecionova","vit-rakusan","vlastimil-valek","vojtech-pikal","zdenek-podal"
};

        public static string[] Poslanci2017Vsichni = new string[] {
"adam-kalous-1","adam-vojtech-1","alena-gajduskova","ales-juchelka", "alexander-cerny","andrea-babisova","andrea-brzobohata","andrej-babis",
"antonin-stanek","barbora-koranova-1","bohuslav-svoboda","dana-balcarova-3","daniel-pawlas","david-kasal",
"david-prazak-5","david-stolpa","dominik-feri","eva-fialova-8","frantisek-elfmark-1","frantisek-kopriva-20","frantisek-petrtyl","frantisek-vacha",
"hana-aulicka-jirovcova","helena-langsadlova","helena-valkova","ilona-mauritzova","ivan-adamec","ivan-bartos","ivan-jac-4","ivana-nevludova",
"ivo-pojezny","ivo-vondrak-2","jakub-janda","jakub-michalek","jan-bartosek","jan-bauer","jan-birke","jan-chvojka","jan-cizinsky","jan-farsky",
"jan-hamacek","jan-hrncir","jan-kobza-1","jan-kubik-9","jan-posvar-3","jan-rehounek","jan-richter","jan-schiller","jan-skopecek",
"jan-volny","jan-zahradnik","jana-cernochova","jana-krutakova","jana-levova-7","jana-pastuchova","jana-vildumetzova","jaroslav-bzoch-1",
"jaroslav-dvorak-110","jaroslav-faltynek","jaroslav-foldyna","jaroslav-holik","jaroslav-kytyr","jaroslav-martinu","jiri-behounek",
"jiri-blaha-82","jiri-dolejs","jiri-kohoutek-9","jiri-masek-50","jiri-mihola","jiri-strycek","jiri-valenta",
"jiri-ventruba","josef-belica-2","josef-hajek","josef-kott","julius-spicak","kamal-farhan","karel-krejza","karel-rais","karel-schwarzenberg",
"karel-turecek","karla-marikova","karla-slechtova","katerina-valachova","klara-dostalova","kveta-matusovska","ladislav-oklestek",
"lenka-drazilova-1","lenka-kozlova-4","leo-luzar","lubomir-spanel-1","lubomir-volny-1","lubomir-zaoralek","lucie--safrankova","lukas-barton",
"lukas-cernohorsky","lukas-kolarik-1","marcela-melkova","marek-benda","marek-novak-12","marek-vyborny","margita-balastikova","marian-bojko",
"marian-jurecka","marketa-adamova","martin-baxa","martin-jiranek-1","martin-kolovratnik","michal-ratiborsky",
"mikulas-ferjencik-1","milan-brazdil","milan-feranec","milan-hnilicka-4","miloslav-janulik","miloslav-rozner",
"miloslava-rutova","miloslava-vostra","miroslav-grebenicek","miroslav-kalousek","miroslava-nemcova","monika-jarosova-6","monika-oborna",
"olga-richterova","ondrej-benesik","ondrej-polansky","ondrej-profant","ondrej-vesely","patrik-nacher","pavel-belobradek","pavel-blazek",
"pavel-jelinek","pavel-juricek-4","pavel-kovacik","pavel-plzak","pavel-pustejovsky","pavel-ruzicka-19","pavel-stanek-1","pavel-zacek-18",
"pavla-golasowska","petr-beitl-1","petr-bendl","petr-dolinek","petr-fiala","petr-gazdik","petr-sadovsky-4","petr-tresnak-12","petr-vrana-2",
"milan-pour","premysl-malis-1","radek-holomcik","radek-koten","radek-rozvoral","radek-vondracek","radek-zlesak","radim-fiala",
"radovan-vich-1","richard-brabec","robert-kralicek","roman-kubicek","roman-onderka","rostislav-vyzula",
"stanislav-berkovec","stanislav-blaha","stanislav-fridrich-2","stanislav-grospic","stanislav-juranek","tatana-mala-2","tereza-hythova",
"tomas-hanzel","tomas-kohoutek-7","tomas-martinek-10","tomas-vymazal-7","tomio-okamura","vaclav-klaus-2","vera-adamkova-1","vera-kovarova",
"vera-prochazkova-19","vit-kankovsky","vit-rakusan","vlastimil-valek","vojtech-filip","vojtech-munzar",
"vojtech-pikal","zbynek-stanjura","zdenek-ondracek","zdenek-podal","zuzana-majerova-zahradnikova","zuzana-ozanova", "ondrej-babka", "petr-beitl",
"josef-belica", "jiri-blaha-82", "monika-cervickova", "lenka-drazilova", "dvorak-jaroslav-1", "mikulas-ferjencik", "milan-hnilicka-1",
"pavel-jelinek,-phd.", "iva-kalatova", "jiri-kobza-2", "martin-kupka", "jan-lipavsky-2", "eva-matyasova", "jana-mrackova-vildumetzova",
"frantisek-navrkal-2", "petr-pavek-8", "marketa-pekarova-adamova", "marie-pencikova", "roman-sklenak", "petr-venhoda", "ivo-vondrak", "vaclav-votava",
"pavel-zacek"
};

        public static string[] Vlada2017 = new string[] {
"andrej-babis","richard-brabec","dan-tok","","marta-novakova-29",
"jan-hamacek","jana-malacova","tomas-petricek","alena-schillerova","lubomir-metnar","adam-vojtech-1","petr-krcal",
"tatana-mala-2","robert-plaga","klara-dostalova","miroslav-toman","antonin-stanek"
    }; //Pridat Jan Kněžínek	


        public static string[] Poslanci2021Novacci = new string[] {
            "jana-bacikova","vladimir-balas","romana-belohlavkova","roman-belor","jan-berki","jana-berkovcova",
            "josef-bernard","lubomir-broz-3","eva-decroix","tomas-dubsky-10","ales-dufek","martin-exner-1","petr-fifka",
            "romana-fischerova-1","josef-flek6","karel-haas","jiri-hajek-62","martin-hajek-2","jana-hanzlikova",
            "matej-ondrej-havel","jiri-havranek-25","tomas-helebrant-3","simon-heller","igor-hendrych-3","jan-hofmann",
            "jiri-horak","marie-jilkova","pavel-kasnik","zdenek-kettner-3","pavel-klima-31","lenka-knechtova",
            "klara-kocmanova","michael-kohajda","ondrej-kolar","vaclav-kral","jan-kuchar-13","martin-kukla-1",
            "jan-lacina-1","hubert-lang","petr-letocha","martina-lisova","petr-liska","ondrej-lochman","ivana-madlova",
            "martin-major","lubomir-metnar","tomas-muller-39","hana-naiclerova","jiri-navratil",
            "zdenka-nemeckova-crkvenjas","milos-novy","hayato-okamura","eliska-olsakova","michaela-opltova",
            "renata-oulehlova-1","berenika-pestova-1","tom-philipp","pavla-vankova-4","marie-posarova",
            "lucie-potuckova","petra-quittova","michael-rataj","drahoslav-ryba","rudolf-salvetr","jan-sila-10",
            "karel-sladecek-1","jiri-slavik-28","karel-smetana-16","robert-strzinek","pavel-svoboda-199",
            "michaela-sebelova-1","david-simek-15","iveta-stefanova-1","robert-teleky-1","antonin-tesarik",
            "libor-turek","barbora-urbanova-5","lukas-vlcek","milada-voborska","viktor-vojtko","vit-vomacka",
            "lubomir-wenzl","milan-wenzl","renata-zajickova","miroslav-zborovsky-2","vladimir-zlinsky","michal-zuna"
        };

        public static string[] Poslanci2021Vsichni = new string[] {
            "alena-schillerova","ales-dufek","ales-juchelka","andrea-babisova","andrej-babis","antonin-tesarik",
            "barbora-urbanova-5","berenika-pestova-1","bohuslav-svoboda","cerny-oldrich","david-kasal","david-prazak-5",
            "david-simek-15","david-stolpa","drahoslav-ryba","dvorak-jaroslav-1","eliska-olsakova","eva-decroix",
            "eva-fialova-8","frantisek-petrtyl","hana-naiclerova","hayato-okamura","helena-langsadlova","helena-valkova",
            "hubert-lang","igor-hendrych-3","ivan-adamec","ivan-bartos","ivan-jac-4","ivana-madlova","iveta-stefanova-1",
            "ivo-vondrak","jakub-janda","jakub-michalek","jan-bartosek","jan-bauer","berenika-pestova-1","jan-bures",
            "jan-farsky","jan-hofmann","jan-hrncir","jan-jakob","jan-kubik-9","jan-kuchar-13","jan-lacina-1","jan-richter",
            "jan-sila-10","jan-skopecek","jan-volny","jana-bacikova","jana-berkovcova","jana-cernochova","jana-hanzlikova",
            "jana-krutakova","jana-mrackova-vildumetzova","jana-pastuchova","jarmila-levko","jaromir-dedecek-1","jaroslav-basta",
            "jaroslav-bzoch-1","jaroslav-faltynek","jaroslav-foldyna","jaroslava-pokorna-jermanova","jiri-carbol",
            "jiri-hajek-62","jiri-havranek-25","jiri-horak","jiri-kobza-2","jiri-masek-50","jiri-navratil","jiri-slavik-28",
            "jiri-strycek","josef-belica","josef-bernard","josef-cogan","josef-flek-6","josef-kott","josef-vana-25",
            "julius-spicak","kamal-farhan","kamila-blahova-3","karel-haas","karel-havlicek-3","karel-krejza","karel-rais",
            "karel-sladecek-1","karel-smetana-16","karel-turecek","karla-marikova","klara-dostalova","klara-kocmanova",
            "ladislav-oklestek","lenka-drazilova","lenka-knechtova","libor-turek","lubomir-broz-3","lubomir-metnar",
            "lubomir-wenzl","lucie--safrankova","lucie-potuckova","lukas-vlcek","marcel-dlask","marek-benda","marek-novak-12",
            "marek-vyborny","marek-zenisek","margita-balastikova","marian-jurecka","marie-jilkova","marie-posarova",
            "marketa-pekarova-adamova","martin-baxa","martin-dlouhy","martin-exner-1","martin-hajek-2","martin-kolovratnik",
            "martin-kukla-1","martin-kupka","martin-major","martina-ochodnicka","matej-ondrej-havel","michael-kohajda",
            "michael-rataj","michaela-opltova","michaela-sebelova-1","michal-kucera","michal-ratiborsky","michal-zuna",
            "milada-voborska","milan-brazdil","milan-feranec","milan-wenzl","milos-novy","miloslav-janulik",
            "miroslav-zborovsky-2","monika-oborna","nina-novakova","olga-richterova","ondrej-babka","ondrej-benesik",
            "ondrej-kolar","ondrej-lochman","patrik-nacher","pavel-belobradek","pavel-blazek","pavel-kasnik","pavel-klima-31",
            "pavel-ruzicka-19","pavel-stanek","pavel-svoboda-199","pavel-zacek","pavla-golasowska","pavla-vankova-4",
            "petr-beitl","petr-bendl","petr-fiala","petr-fifka","petr-gazdik","petr-letocha","petr-liska","petr-sadovsky-4",
            "petr-vrana-2","petra-quittova","radek-koten","radek-rozvoral","radek-vondracek","radim-fiala","radovan-vich-1",
            "renata-oulehlova-1","renata-zajickova","richard-brabec","robert-kralicek","robert-strzinek","robert-teleky",
            "roman-belor","roman-kubicek","romana-belohlavkova","romana-fischerova-1","rudolf-salvetr","silvia-dousova",
            "simon-heller","stanislav-berkovec","stanislav-blaha","stanislav-fridrich-2","tatana-mala-2","tom-philipp",
            "tomas-dubsky-10","tomas-helebrant-3","tomas-kohoutek-7","tomas-muller-39","tomio-okamura","vaclav-kral",
            "vera-adamkova-1","vera-kovarova","viktor-vojtko","vit-kankovsky","vit-rakusan","vit-vomacka","vladimir-balas",
            "vladimir-zlinsky","vladimira-lesenska","vlastimil-valek","vojtech-munzar","zbynek-stanjura","zdenek-kettner-3",
            "zdenka-nemeckova-crkvenjas","zuzana-ozanova","jan-berki","radim-holis","bohuslav-hudec","radim-jirout",
            "miroslav-samas"
        };
        public static Osoba[] Poslanci2021VsichniOsoby = Poslanci2021Vsichni
            .Select(m=>Osoby.GetByNameId.Get(m))
            .ToArray();

        public static string[] Poslanci2021OdesliDrive = new string[] {
            "jan-farsky",
            "david-simek-15",
            "jana-mrackova-vildumetzova",
            "jaroslav-basta",
            "jaroslav-bzoch-1",
            "jaroslava-pokorna-jermanova",
            "klara-dostalova",
            "milan-feranec",
            "ondrej-kolar",
            "ondrej-lochman",
            "radim-holis"
        };

        public static string[] Poslanci2021NastoupiliPozdeji = new string[] {
            "jaromir-dedecek-1",
            "kamila-blahova-3",
            "marcel-dlask",
            "martin-dlouhy",
            "silvia-dousova",
            "josef-vana-25",
            "jarmila-levko",
            "margita-balastikova",
            "bohuslav-hudec",
            "radim-jirout",
            "miroslav-samas"
        };

        static StaticData()
        {
            Init();
        }


        public static void Init()
        {
            string appDataPath = Connectors.Init.WebAppDataPath;

            lock (lockObj)
            {
                if (initialized)
                    return;

                _logger.Information("Static data - Init start");
                //TweetingManager = new SingletonManagerWithSetup<Data.External.TwitterPublisher, Tweetinvi.Models.TwitterCredentials>();

                if (string.IsNullOrEmpty(appDataPath))
                {
                    throw new ArgumentNullException("appDataPath");
                }
                Dumps_Path = Devmasters.Config.GetWebConfigValue("DumpsPath");
                if (string.IsNullOrEmpty(Dumps_Path))
                    throw new ArgumentNullException(".config param DumpsPath missing");
                if (!Dumps_Path.EndsWith(Path.DirectorySeparatorChar))
                    Dumps_Path = Dumps_Path + Path.DirectorySeparatorChar;
                Directory.CreateDirectory(Dumps_Path);


                _logger.Information("Static data - NespolehlivyPlatciDPH ");
                NespolehlivyPlatciDPH = new Devmasters.Cache.LocalMemory.Cache<Dictionary<string, NespolehlivyPlatceDPH>>
                    (TimeSpan.FromHours(12), "NespolehlivyPlatciDPH",
                    (o) =>
                    {
                        var data = NespolehlivyPlatceDphRepo.GetAllFromDb();
                        if (data.Count == 0)
                        {
                            NespolehlivyPlatceDphRepo.UpdateData();
                            data = NespolehlivyPlatceDphRepo.GetAllFromDb();
                        }
                        return data;
                    });

                _logger.Information("Static data - Insolvence_firem_politiku ");
                Insolvence_firem_politiku_Cache = new Devmasters.Cache.AWS_S3.Cache<Tuple<Osoba.Statistics.RegistrSmluv, Entities.Insolvence.RizeniStatistic[]>[]>(
                    new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero,
                    "Insolvence_firem_politiku", (obj) =>
                     {
                         var ret = new List<Tuple<Osoba.Statistics.RegistrSmluv, Entities.Insolvence.RizeniStatistic[]>>();
                         var lockObj = new object();
                         Devmasters.Batch.Manager.DoActionForAllAsync<Osoba>(OsobaRepo.PolitickyAktivni.Get().Where(m => m.StatusOsoby() == Osoba.StatusOsobyEnum.Politik).Distinct(), async (o) =>
                             {
                                 var icos = o.AktualniVazby(Relation.AktualnostType.Nedavny)
                                                 .Where(w => !string.IsNullOrEmpty(w.To.Id))
                                                 //.Where(w => Analysis.ACore.GetBasicStatisticForICO(w.To.Id).Summary.Pocet > 0)
                                                 .Select(w => w.To.Id);
                                 if (icos.Count() > 0)
                                 {
                                     var res = await InsolvenceRepo.Searching.SimpleSearchAsync("osobaiddluznik:" + o.NameId, 1, 100,
                                            (int)Repositories.Searching.InsolvenceSearchResult.InsolvenceOrderResult.LatestUpdateDesc,
                                            limitedView: false);
                                     if (res.IsValid && res.Total > 0)
                                     {
                                         var insolvenceIntoList = new List<Entities.Insolvence.Rizeni>();
                                         foreach (var i in res.ElasticResults.Hits.Select(m => m.Source))
                                         {
                                             bool addToList = false;
                                             var pdluznici = i.Dluznici.Where(m => icos.Contains(m.ICO));
                                             if (pdluznici.Count() > 0)
                                             {
                                                 foreach (var pd in pdluznici)
                                                 {
                                                     Firma f = Firmy.Get(pd.ICO);
                                                     var vazby = o.VazbyProICO(pd.ICO);
                                                     foreach (var v in vazby)
                                                     {
                                                         if (Devmasters.DT.Util.IsOverlappingIntervals(i.DatumZalozeni, i.PosledniZmena, v.RelFrom, v.RelTo))
                                                         {
                                                             addToList = true;
                                                             goto addList;
                                                         }
                                                     }
                                                 }
                                             }
                                         addList:
                                             if (addToList)
                                                 insolvenceIntoList.Add(i);
                                         }
                                         if (insolvenceIntoList.Count() > 0)
                                         {
                                             lock (lockObj)
                                             {
                                                 Osoba.Statistics.RegistrSmluv stat = o.StatistikaRegistrSmluv(Relation.AktualnostType.Nedavny);

                                                 ret.Add(new Tuple<Osoba.Statistics.RegistrSmluv, Entities.Insolvence.RizeniStatistic[]>(
                                                                     stat, insolvenceIntoList
                                                                             .Select(m => new Entities.Insolvence.RizeniStatistic(m, icos))
                                                                             .ToArray()
                                                                     )
                                                     );
                                             }
                                         }

                                     }
                                 }
                                 return new Devmasters.Batch.ActionOutputData();
                             },
                             Util.Consts.outputWriter.OutputWriter,
                             Util.Consts.progressWriter.ProgressWriter,
                             true, //!System.Diagnostics.Debugger.IsAttached,
                             maxDegreeOfParallelism: 6, prefix: "Insolvence politiku ", monitor: new MonitoredTaskRepo.ForBatch())
                             .ConfigureAwait(false).GetAwaiter().GetResult();

                         return ret.ToArray();
                     }
                    );

                _logger.Information("Static data - SponzorujiciFirmy_Vsechny ");


                SponzorujiciFirmy_Vsechny = new Devmasters.Cache.LocalMemory.AutoUpdatedCache<List<Sponzoring>>(
                                TimeSpan.FromHours(3), (obj) =>
                                {
                                    List<Sponzoring> dary = null;
                                    DateTime limit10let = new DateTime(DateTime.Now.Year, 1, 1).AddYears(-10);
                                    using (DbEntities db = new DbEntities())
                                    {
                                        dary = db.Sponzoring
                                            .AsNoTracking()
                                            .Where(s => s.IcoDarce != null
                                                && s.DarovanoDne > limit10let
                                                && s.IcoPrijemce != null) //pro zachování funkčnosti
                                            .ToList();

                                        return dary;
                                    }
                                }
                            );

                _logger.Information("Static data - SponzorujiciFirmy_nedavne");
                SponzorujiciFirmy_Nedavne = new Devmasters.Cache.LocalMemory.AutoUpdatedCache<List<Sponzoring>>(
                        TimeSpan.FromHours(3), (obj) =>
                        {
                            return SponzorujiciFirmy_Vsechny.Get()
                                    .Where(m =>
                                        (m.DarovanoDne.HasValue && m.DarovanoDne.Value.Add(Relation.NedavnyVztahDelka) > DateTime.Now)
                                        ||
                                        (m.DarovanoDne.HasValue && m.DarovanoDne.Value.Add(Relation.NedavnyVztahDelka) > DateTime.Now)
                                    )
                                    .ToList();
                        }
                    );

                //if (!System.Diagnostics.Debugger.IsAttached)
                OsobaRepo.PolitickyAktivni.Get(); //force to load
                SponzorujiciFirmy_Vsechny.Get(); //force to load

                _logger.Information("Static data - DarujmeStats");

                DarujmeStats = new Devmasters.Cache.LocalMemory.Cache<Darujme.Stats>(
                        TimeSpan.FromHours(3), (obj) =>
                        {
                            var defData = new Darujme.Stats()
                            {
                                projectStats = new Darujme.Stats.Projectstats()
                                {
                                    collectedAmountEstimate = new Darujme.Stats.Projectstats.Collectedamountestimate()
                                    {
                                        cents = 70891100,
                                        currency = "CZK"
                                    },
                                    donorsCount = 280,
                                    projectId = 1200384
                                }
                            };
                            try
                            {
                                using (Devmasters.Net.HttpClient.URLContent url = new Devmasters.Net.HttpClient.URLContent("https://www.darujme.cz/api/v1/project/1200384/stats?apiId=74233883&apiSecret=q2vqimypo2ohpa0qi6g9zwn37rb1bpaan12gulqk"))
                                {
                                    return Newtonsoft.Json.JsonConvert.DeserializeObject<Darujme.Stats>(url.GetContent().Text);
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.Error(e, "Static data - DarujmeStats");

                                return defData;
                            }
                        }
                    );

                _logger.Information("Static data - BasicStatisticData ");
                BasicStatisticData = new Devmasters.Cache.LocalMemory.Cache<List<double>>(
                        TimeSpan.FromHours(6), (obj) =>
                        {
                            List<double> pol = new List<double>();
                            try
                            {
                                var res = SmlouvaRepo.Searching.RawSearchAsync("", 1, 0, platnyZaznam: true, anyAggregation:
                                    new Nest.AggregationContainerDescriptor<Smlouva>()
                                        .Sum("totalPrice", m => m
                                            .Field(ff => ff.CalculatedPriceWithVATinCZK)
                                    ), exactNumOfResults: true
                                    ).ConfigureAwait(false).GetAwaiter().GetResult();
                                var resNepl = SmlouvaRepo.Searching.RawSearchAsync("", 1, 0, platnyZaznam: false, anyAggregation:
                                    new Nest.AggregationContainerDescriptor<Smlouva>()
                                        .Sum("totalPrice", m => m
                                            .Field(ff => ff.CalculatedPriceWithVATinCZK)
                                    ), exactNumOfResults: true
                                    ).ConfigureAwait(false).GetAwaiter().GetResult();

                                long platnych = res.Total;
                                long neplatnych = resNepl.Total; ;
                                double celkemKc = 0;

                                celkemKc = ((Nest.ValueAggregate)res.Aggregations["totalPrice"]).Value.Value;

                                pol.Add(platnych);
                                pol.Add(neplatnych);
                                pol.Add(celkemKc);
                                return pol;
                            }
                            catch (Exception)
                            {
                                pol.Add(0);
                                pol.Add(0);
                                pol.Add(0);
                                return pol;
                            }
                        }

                    );







                _logger.Information("Static data - FirmySVazbamiNaPolitiky_*");
                FirmySVazbamiNaPolitiky_aktualni_Cache = new Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaPolitiky>
                   (new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero,
                     "FirmySVazbamiNaPolitiky_Aktualni",
                   (o) =>
                       {
                           return AnalysisCalculation.LoadFirmySVazbamiNaPolitiky(Relation.AktualnostType.Aktualni, true);
                       });

                FirmySVazbamiNaPolitiky_nedavne_Cache = new Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaPolitiky>
                   (new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero,

                   "FirmySVazbamiNaPolitiky_Nedavne",
                   (o) =>
                   {
                       return new AnalysisCalculation.VazbyFiremNaPolitiky();

                   });

                FirmySVazbamiNaPolitiky_vsechny_Cache = new Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaPolitiky>
                   (
                    new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero,
                    "FirmySVazbamiNaPolitiky_Vsechny",
                   (o) =>
                   {
                       return AnalysisCalculation.LoadFirmySVazbamiNaPolitiky(Relation.AktualnostType.Libovolny, true);
                   });


                FirmyCasovePodezreleZalozene = new Devmasters.Cache.AWS_S3.Cache<IEnumerable<AnalysisCalculation.IcoSmlouvaMinMax>>
                   (new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero,
                    "FirmyCasovePodezreleZalozene",
                   (o) =>
                   {
                       return AnalysisCalculation.GetFirmyCasovePodezreleZalozeneAsync()
                           .ConfigureAwait(false).GetAwaiter().GetResult();
                   });

                //migrace: tohle by mělo jít odsud do Repo cache
                ZkratkyStran_cache = new Devmasters.Cache.LocalMemory.Cache<Dictionary<string, string>>
                    (TimeSpan.FromHours(24), "ZkratkyStran",
                    (o) =>
                    {
                        return ZkratkaStranyRepo.ZkratkyVsechStran();
                    });


                _logger.Information("Static data - UradyObchodujiciSFirmami_s_vazbouNaPolitiky_*");
                UradyObchodujiciSFirmami_s_vazbouNaPolitiky_aktualni_Cache = new Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaUradyStat>
                    (new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero,
                    "UradyObchodujiciSFirmami_s_vazbouNaPolitiky_aktualni",
                    (o) =>
                    {
                        return AnalysisCalculation.UradyObchodujiciSFirmami_s_vazbouNaPolitikyAsync(Relation.AktualnostType.Aktualni, true)
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    );

                UradyObchodujiciSFirmami_s_vazbouNaPolitiky_nedavne_Cache = new Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaUradyStat>
                    (new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero,
                    "UradyObchodujiciSFirmami_s_vazbouNaPolitiky_nedavne",
                    (o) =>
                    {
                        return AnalysisCalculation.UradyObchodujiciSFirmami_s_vazbouNaPolitikyAsync(Relation.AktualnostType.Nedavny, true)
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    );
                UradyObchodujiciSFirmami_s_vazbouNaPolitiky_vsechny_Cache = new Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaUradyStat>
                    (new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero,

                    "UradyObchodujiciSFirmami_s_vazbouNaPolitiky_vsechny",
                    (o) =>
                    {
                        return AnalysisCalculation.UradyObchodujiciSFirmami_s_vazbouNaPolitikyAsync(Relation.AktualnostType.Libovolny, true)
                            .ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    );

                _logger.Information("Static data - UradyObchodujiciSNespolehlivymiPlatciDPH_Cache*");
                UradyObchodujiciSNespolehlivymiPlatciDPH_Cache = new Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaUradyStat>
                    (new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero,

                    "UradyObchodujiciSNespolehlivymiPlatciDPH",
                    (o) =>
                    {
                        return new AnalysisCalculation.VazbyFiremNaUradyStat(); //refresh from task
                    }
                    );
                NespolehlivyPlatciDPH_obchodySurady_Cache = new Devmasters.Cache.AWS_S3.Cache<AnalysisCalculation.VazbyFiremNaUradyStat>
                    (new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"), TimeSpan.Zero,

                    "NespolehlivyPlatciDPH_obchodySurady",
                    (o) =>
                    {
                        return new AnalysisCalculation.VazbyFiremNaUradyStat(); //refresh from task
                    }
                    );



                //AFERY
                Afery.Add("parlamentnilisty", new TemplatedQuery()
                {
                    Text = "Inzerce na Parlamentních listech",
                    Description = "Které úřady inzerují na serveru Parlamentní listy? Smlouvy s inzercí na médiích Our Media a.s. (vydavatel Parlamentních listů) anebo s mediaplánem přímo pro PL. Částky ze smluv jsou orientační a mohou obsahovat objednávky i na jiná média.",
                    Query = "\"OUR MEDIA\" OR \"Parlamentní listy\" OR \"Parlamentnilisty.cz\" OR ico:28876890 OR \"KrajskeListy.cz\" OR \"Krajské listy\" OR \"Prvnizpravy.cz\" OR icoPrijemce:24214868",
                    UrlTemplate = "/HledatSmlouvy?Q={0}",
                    Links = new TemplatedQuery.AHref[]{
                        new TemplatedQuery.AHref("https://web.archive.org/web/20170102215202/http://mediahub.cz/komunikace-35809/clanky-v-parlamentnich-listech-si-plati-ministri-i-hejtmani-celkove-castky-jdou-do-milionu-inzerce-vypada-jako-redakcni-clanky-1058528",
                        "Články v Parlamentních listech si platí ministři i hejtmani. Celkové částky jdou do milionů. Inzerce vypadá jako redakční články"),
                        new TemplatedQuery.AHref("https://denikn.cz/39674/valentuv-server-zautocil-na-transparency-hned-pote-co-ho-narkla-ze-stretu-zajmu-nahoda-rika-senator/",
                        "Valentův server zaútočil na Transparency hned poté, co ho nařkla ze střetu zájmů.")
                    }
                });
                Afery.Add("uklid-praha-cssd", new TemplatedQuery()
                {
                    Text = "Úklidové služby pro firmy členů ČSSD",
                    Description = "Zakázky pro firmy Premio Invest a Lasesmed, které vlastní členové ČSSD a roky dostávaly stovky milionů za úklid v Praze od městských organizací, kde mají vliv sociálnědemokratičtí politici.",
                    Query = "ico:26746590 OR ico:28363809",
                    UrlTemplate = "/HledatSmlouvy?Q={0}",
                    Links = new TemplatedQuery.AHref[]{
                        new TemplatedQuery.AHref("https://zpravy.aktualne.cz/domaci/uklid-prahy-jako-stranicky-byznys-clenove-cssd-vlastni-firmy/r~328647c27c2811e7954a002590604f2e/","Úklid Prahy jako byznys ČSSD. Její členové mají firmy, které žijí ze stamilionových zakázek od města"),
                    }
                });
                Afery.Add("eet", new TemplatedQuery()
                {
                    Text = "EET",
                    Description = "Smlouvy pokrývající vývoj a provoz EET. ",
                    Query = "(ico:03630919 OR ico:72054506 OR ico:72080043) AND (EET OR ADIS)",
                    UrlTemplate = "/HledatSmlouvy?Q={0}",
                    Links = new TemplatedQuery.AHref[]{
                        new TemplatedQuery.AHref("https://dotyk.denik.cz/publicistika/eet-babisuv-nepruhledny-system-na-vymahani-dani-20160915.html","EET: Babišův neprůhledný systém na vymáhání daní"),
                        new TemplatedQuery.AHref("https://www.hlidacstatu.cz/texty/10x-predrazene-eet-skutecne-naklady-na-eet/","10x předražené EET: skutečné náklady na EET"),
                    }
                });
                Afery.Add("elektronicke-myto", new TemplatedQuery()
                {
                    Text = "Elektronické mýto",
                    Description = @"Smlouvy související elektronickým mýtem.",
                    Query = "\"elektronické mýto\"",
                    UrlTemplate = "/HledatSmlouvy?Q={0}"
                });
                Afery.Add("rsd-s-omezenou-soutezi", new TemplatedQuery()
                {
                    Text = "Smlouvy ŘSD s omezenou soutěží",
                    Description = @"Smlouvy ŘSD uzavřené v užším řízení či v jednacím řízení bez uveřejnění.",
                    Query = "icoPlatce:65993390 AND ( \"stavební práce v užším řízení\" OR \"jednací řízení bez uveřejnění\") ",
                    UrlTemplate = "/HledatSmlouvy?Q={0}"
                });
                Afery.Add("clenove-ictunie", new TemplatedQuery()
                {
                    Text = "Smlouvy členů ICT Unie",
                    Description = @"Smlouvy uzavřené členy ICT Unie a jejich dcerami.",
                    Query = "holding:00174939 OR holding:00533394 OR holding:00672416 OR holding:03136108 OR holding:04308697 OR holding:07366949 OR holding:09702652 OR holding:14482282 OR holding:14890992 OR holding:16188781 OR holding:17007909 OR holding:24738875 OR holding:24775487 OR holding:25146441 OR holding:25618067 OR holding:25625632 OR holding:25648101 OR holding:25788001 OR holding:26129558 OR holding:26298953 OR holding:26426331 OR holding:26482347 OR holding:26500281 OR holding:26851652 OR holding:27074358 OR holding:27367061 OR holding:27604977 OR holding:28606582 OR holding:40527514 OR holding:40766314 OR holding:43871020 OR holding:44797320 OR holding:44846029 OR holding:44851391 OR holding:45272808 OR holding:45786259 OR holding:47117087 OR holding:47123737 OR holding:47451084 OR holding:47903783 OR holding:48591254 OR holding:60193336 OR holding:61060631 OR holding:61498084 OR holding:61498483 OR holding:61855057 OR holding:62028235 OR holding:62911643 OR holding:63078236 OR holding:63911035 OR holding:63979462 OR holding:64949541",
                    UrlTemplate = "/Hledat?Q={0}"
                });
                Afery.Add("ictunie", new TemplatedQuery()
                {
                    Text = "Smlouvy členů ICT Unie",
                    Description = @"Smlouvy uzavřené členy ICT Unie a jejich dcerami.",
                    Query = "holding:00174939 OR holding:00533394 OR holding:00672416 OR holding:03136108 OR holding:04308697 OR holding:07366949 OR holding:09702652 OR holding:14482282 OR holding:14890992 OR holding:16188781 OR holding:17007909 OR holding:24738875 OR holding:24775487 OR holding:25146441 OR holding:25618067 OR holding:25625632 OR holding:25648101 OR holding:25788001 OR holding:26129558 OR holding:26298953 OR holding:26426331 OR holding:26482347 OR holding:26500281 OR holding:26851652 OR holding:27074358 OR holding:27367061 OR holding:27604977 OR holding:28606582 OR holding:40527514 OR holding:40766314 OR holding:43871020 OR holding:44797320 OR holding:44846029 OR holding:44851391 OR holding:45272808 OR holding:45786259 OR holding:47117087 OR holding:47123737 OR holding:47451084 OR holding:47903783 OR holding:48591254 OR holding:60193336 OR holding:61060631 OR holding:61498084 OR holding:61498483 OR holding:61855057 OR holding:62028235 OR holding:62911643 OR holding:63078236 OR holding:63911035 OR holding:63979462 OR holding:64949541",
                    UrlTemplate = "/Hledat?Q={0}"
                });


                // hierarchie uradu
                try
                {
                    OrganizacniStrukturyUraduCache = new Devmasters.Cache.AWS_S3.AutoUpdatebleCache<OrganizacniStrukturyUradu>(
                                            new string[] { Devmasters.Config.GetWebConfigValue("Minio.Cache.Endpoint") },
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.Bucket"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.AccessKey"),
                    Devmasters.Config.GetWebConfigValue("Minio.Cache.SecretKey"),
                    TimeSpan.FromDays(90),
                    "OrganizacniStrukturyUradu", (obj) =>
                        {
                            OrganizacniStrukturyUradu res = new OrganizacniStrukturyUradu();
                            var ossu = ParseOssu();

                            res.PlatneKDatu = ossu.ExportInfo.ExportDatumCas;

                            var _organizaniStrukturyUradu = new Dictionary<string, List<JednotkaOrganizacni>>();
                            try
                            {

                                foreach (var urad in ossu.UradSluzebniSeznam.SluzebniUrady)
                                {
                                    var f = FirmaRepo.FromDS(urad.idDS);
                                    if (f is null || !f.Valid)
                                    {
                                        if (string.IsNullOrEmpty(urad.idNadrizene))
                                        {
                                            _logger.Error($"Organizační struktura - nenalezena datová schránka [{urad.idDS}] úřadu [{urad.oznaceni}]");
                                            continue;
                                        }

                                        var nadrizeny = ossu.UradSluzebniSeznam.SluzebniUrady
                                            .Where(u => u.id == urad.idNadrizene)
                                            .FirstOrDefault();

                                        if (nadrizeny is null)
                                        {
                                            _logger.Error($"Nenalezen nadřízený úřad, ani datová schránka [{urad.idDS}] úřadu [{urad.oznaceni}]");
                                            continue;
                                        }

                                        f = FirmaRepo.FromDS(nadrizeny.idDS);
                                        if (f is null || !f.Valid)
                                        {
                                            _logger.Error($"Organizační struktura - nenalezena datová schránka [{nadrizeny.idDS}] nadřízeného úřadu [{nadrizeny.oznaceni}]");
                                            continue;
                                        }
                                    }

                                    var sluzebniUrad = ossu.OrganizacniStruktura.Where(os => os.id == urad.id)
                                        .FirstOrDefault()
                                        ?.StrukturaOrganizacni?.HlavniOrganizacniJednotka;

                                    if (sluzebniUrad is null)
                                    {
                                        _logger.Information($"Služební úřad [{urad.oznaceni}] nemá podřízené organizace.");
                                        continue;
                                    }

                                    if (_organizaniStrukturyUradu.TryGetValue(f.ICO, out var sluzebniUrady))
                                    {
                                        sluzebniUrady.Add(sluzebniUrad);
                                    }
                                    else
                                    {
                                        _organizaniStrukturyUradu.Add(f.ICO, new List<JednotkaOrganizacni>()
                                            {
                                                    sluzebniUrad
                                            });
                                    }

                                }


                            }
                            catch (Exception ex)
                            {
                                _logger.Error($"Chyba záznamu při zpracování struktury úřadů. {ex}");
                            }

                            res.Urady = _organizaniStrukturyUradu;
                            return res;
                        }, null);

                }
                catch (Exception ex)
                {
                    _logger.Error($"Chyba při zpracování struktury úřadů. {ex}");
                }



                initialized = true;
            } //lock
            _logger.Information("Static data - Init DONE");
        }

        private static organizacni_struktura_sluzebnich_uradu ParseOssu()
        {
            try
            {

                string url = "https://portal.isoss.gov.cz/opendata/ISoSS_Opendata_OSYS_OSSS.xml";
                string xml = Devmasters.Net.HttpClient.Simple.GetAsync(url).Result;

                var ser = new XmlSerializer(typeof(organizacni_struktura_sluzebnich_uradu));

                using (var streamReader = new StringReader(xml))
                {
                    using (var reader = XmlReader.Create(streamReader))
                    {
                        return (organizacni_struktura_sluzebnich_uradu)ser.Deserialize(reader);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "organizacni_struktura_sluzebnich_uradu ParseOssu");
                return new organizacni_struktura_sluzebnich_uradu();
            }

        }


    }
}