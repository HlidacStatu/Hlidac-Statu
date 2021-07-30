using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.OrgStrukturyStatu;
using HlidacStatu.Extensions;
using Microsoft.EntityFrameworkCore; //using HlidacStatu.Extensions;

namespace HlidacStatu.Repositories
{

    public static class StaticData
    {
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
        
        
        public static Devmasters.Cache.File.FileCache<Dictionary<string, List<JednotkaOrganizacni>>> OrganizacniStrukturyUradu = null;


        

        public static Devmasters.Cache.File.FileCache<IEnumerable<AnalysisCalculation.IcoSmlouvaMinMax>> FirmyCasovePodezreleZalozene = null;
        public static Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_aktualni_Cache = null;
        public static Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_nedavne_Cache = null;
        public static Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaPolitiky> FirmySVazbamiNaPolitiky_vsechny_Cache = null;

        public static Devmasters.Cache.File.BinaryFileCache Autocomplete_Cache = null;
        public static Devmasters.Cache.File.FileCache<List<Autocomplete>> Autocomplete_Firmy_Cache = null;
        public static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<FullTextSearch.Index<Autocomplete>> FulltextSearchForAutocomplete = null;


        public static Devmasters.Cache.File.FileCache<Tuple<Osoba.Statistics.RegistrSmluv, Entities.Insolvence.RizeniStatistic[]>[]> Insolvence_firem_politiku_Cache = null;

        public static Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaUradyStat> UradyObchodujiciSFirmami_s_vazbouNaPolitiky_aktualni_Cache = null;
        public static Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaUradyStat> UradyObchodujiciSFirmami_s_vazbouNaPolitiky_nedavne_Cache = null;
        public static Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaUradyStat> UradyObchodujiciSFirmami_s_vazbouNaPolitiky_vsechny_Cache = null;

        public static Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaUradyStat> UradyObchodujiciSNespolehlivymiPlatciDPH_Cache = null;
        public static Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaUradyStat> NespolehlivyPlatciDPH_obchodySurady_Cache = null;
        
        public static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<List<Sponzoring>> SponzorujiciFirmy_Vsechny = null;
        public static Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<List<Sponzoring>> SponzorujiciFirmy_Nedavne = null;

        public static Devmasters.Cache.LocalMemory.LocalMemoryCache<List<double>> BasicStatisticData = null;

        
        public static Dictionary<string, Lib.Analysis.TemplatedQuery> Afery = new Dictionary<string, Lib.Analysis.TemplatedQuery>();

        public static Devmasters.Cache.LocalMemory.LocalMemoryCache<Dictionary<string, NespolehlivyPlatceDPH>> NespolehlivyPlatciDPH = null;

        //public static SingletonManagerWithSetup<Data.External.TwitterPublisher, Tweetinvi.Models.TwitterCredentials> TweetingManager = null;

        public static Devmasters.Cache.LocalMemory.LocalMemoryCache<Darujme.Stats> DarujmeStats = null;

        public static Devmasters.Cache.LocalMemory.LocalMemoryCache<Dictionary<string, string>> ZkratkyStran_cache = null;


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

                Util.Consts.Logger.Info("Static data - Init start");
                //TweetingManager = new SingletonManagerWithSetup<Data.External.TwitterPublisher, Tweetinvi.Models.TwitterCredentials>();

                if (string.IsNullOrEmpty(appDataPath))
                {
                    throw new ArgumentNullException("appDataPath");
                }
                Dumps_Path = Devmasters.Config.GetWebConfigValue("DumpsPath");
                if (string.IsNullOrEmpty(Dumps_Path))
                    throw new ArgumentNullException(".config param DumpsPath missing");
                if (!Dumps_Path.EndsWith(@"\"))
                    Dumps_Path = Dumps_Path + @"\";
                Directory.CreateDirectory(Dumps_Path);
                

                Util.Consts.Logger.Info("Static data - NespolehlivyPlatciDPH ");
                NespolehlivyPlatciDPH = new Devmasters.Cache.LocalMemory.LocalMemoryCache<Dictionary<string, NespolehlivyPlatceDPH>>
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


                //HlidacStatu.Util.Consts.Logger.Info("Static data - FirmyStatsCache ");
                //FirmyStatsCache = new Devmasters.Cache.File.FileCache<Dictionary<string, QueryStatistic.StatData>>
                //        (StaticData.App_Data_Path, TimeSpan.Zero, "FirmyStats",
                //            null //don't calculate new content. It's too time comsuming
                //        );



                Util.Consts.Logger.Info("Static data - Insolvence_firem_politiku ");
                Insolvence_firem_politiku_Cache = new Devmasters.Cache.File.FileCache<Tuple<Osoba.Statistics.RegistrSmluv, Entities.Insolvence.RizeniStatistic[]>[]>(
                    Connectors.Init.WebAppDataPath, TimeSpan.Zero, "Insolvence_firem_politiku", (obj) =>
                     {
                         var ret = new List<Tuple<Osoba.Statistics.RegistrSmluv, Entities.Insolvence.RizeniStatistic[]>>();
                         var lockObj = new object();
                         Devmasters.Batch.Manager.DoActionForAll<Osoba>(OsobaRepo.PolitickyAktivni.Get().Where(m => m.StatusOsoby() == Osoba.StatusOsobyEnum.Politik).Distinct(),
                             (o) =>
                             {
                                 var icos = o.AktualniVazby(Relation.AktualnostType.Nedavny)
                                                 .Where(w => !string.IsNullOrEmpty(w.To.Id))
                                                 //.Where(w => Analysis.ACore.GetBasicStatisticForICO(w.To.Id).Summary.Pocet > 0)
                                                 .Select(w => w.To.Id);
                                 if (icos.Count() > 0)
                                 {
                                     var res = InsolvenceRepo.Searching.SimpleSearch("osobaiddluznik:" + o.NameId, 1, 100,
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
                             maxDegreeOfParallelism: 6);

                         return ret.ToArray();
                     }
                    );

                Util.Consts.Logger.Info("Static data - SponzorujiciFirmy_Vsechny ");


                SponzorujiciFirmy_Vsechny = new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<List<Sponzoring>>(
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

                Util.Consts.Logger.Info("Static data - SponzorujiciFirmy_nedavne");
                SponzorujiciFirmy_Nedavne = new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<List<Sponzoring>>(
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

                Util.Consts.Logger.Info("Static data - DarujmeStats");

                DarujmeStats = new Devmasters.Cache.LocalMemory.LocalMemoryCache<Darujme.Stats>(
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
                                Util.Consts.Logger.Error("Static data - DarujmeStats", e);

                                return defData;
                            }
                        }
                    );

                Util.Consts.Logger.Info("Static data - BasicStatisticData ");
                BasicStatisticData = new Devmasters.Cache.LocalMemory.LocalMemoryCache<List<double>>(
                        TimeSpan.FromHours(6), (obj) =>
                        {
                            List<double> pol = new List<double>();
                            try
                            {
                                var res = SmlouvaRepo.Searching.RawSearch("", 1, 0, platnyZaznam: true, anyAggregation:
                                    new Nest.AggregationContainerDescriptor<Smlouva>()
                                        .Sum("totalPrice", m => m
                                            .Field(ff => ff.CalculatedPriceWithVATinCZK)
                                    ), exactNumOfResults: true
                                    );
                                var resNepl = SmlouvaRepo.Searching.RawSearch("", 1, 0, platnyZaznam: false, anyAggregation:
                                    new Nest.AggregationContainerDescriptor<Smlouva>()
                                        .Sum("totalPrice", m => m
                                            .Field(ff => ff.CalculatedPriceWithVATinCZK)
                                    ), exactNumOfResults: true
                                    );

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


                

                


                Util.Consts.Logger.Info("Static data - FirmySVazbamiNaPolitiky_*");
                FirmySVazbamiNaPolitiky_aktualni_Cache = new Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaPolitiky>
                   (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "FirmySVazbamiNaPolitiky_Aktualni",
                   (o) =>
                       {
                           return AnalysisCalculation.LoadFirmySVazbamiNaPolitiky(Relation.AktualnostType.Aktualni, true);
                       });

                FirmySVazbamiNaPolitiky_nedavne_Cache = new Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaPolitiky>
                   (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "FirmySVazbamiNaPolitiky_Nedavne",
                   (o) =>
                   {
                       return AnalysisCalculation.LoadFirmySVazbamiNaPolitiky(Relation.AktualnostType.Nedavny, true);
                   });

                FirmySVazbamiNaPolitiky_vsechny_Cache = new Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaPolitiky>
                   (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "FirmySVazbamiNaPolitiky_Vsechny",
                   (o) =>
                   {
                       return AnalysisCalculation.LoadFirmySVazbamiNaPolitiky(Relation.AktualnostType.Libovolny, true);
                   });


                FirmyCasovePodezreleZalozene = new Devmasters.Cache.File.FileCache<IEnumerable<AnalysisCalculation.IcoSmlouvaMinMax>>
                   (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "FirmyCasovePodezreleZalozene",
                   (o) =>
                   {
                       return AnalysisCalculation.GetFirmyCasovePodezreleZalozene();
                   });

                Autocomplete_Cache = new Devmasters.Cache.File.BinaryFileCache
                   (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "Autocomplete",
                   (o) =>
                   {
                       Util.Consts.Logger.Info("Starting AutocompleteRepo.GenerateAutocomplete");
                       var autocompleteData = AutocompleteRepo.GenerateAutocomplete();
                       var index = new FullTextSearch.Index<Autocomplete>(autocompleteData);
                       var serialized = index.Serialize();
                       Util.Consts.Logger.Info("End AutocompleteRepo.GenerateAutocomplete");
                       return serialized;
                   });

                FulltextSearchForAutocomplete = new Devmasters.Cache.LocalMemory.AutoUpdatedLocalMemoryCache<FullTextSearch.Index<Autocomplete>>(
                        TimeSpan.FromHours(12),
                        "FulltextSearchForAutocomplete_main",
                        o =>
                        {
                            Util.Consts.Logger.Info("Loading Autocomplete filecache");
                            var results = Autocomplete_Cache.Get();
                            Util.Consts.Logger.Info("Generating Autocomplete memory cache");
                            var index = FullTextSearch.Index<Autocomplete>.Deserialize(results);
                            Util.Consts.Logger.Info("Generating Autocomplete memory cache done");
                            return index;
                        });

                Autocomplete_Firmy_Cache = new Devmasters.Cache.File.FileCache<List<Autocomplete>>
                   (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "Autocomplete_firmyOnly",
                   (o) =>
                   {
                       return AutocompleteRepo.GenerateAutocompleteFirmyOnly().ToList();
                   });

                //migrace: tohle by mělo jít odsud do Repo cache
                ZkratkyStran_cache = new Devmasters.Cache.LocalMemory.LocalMemoryCache<Dictionary<string, string>>
                    (TimeSpan.FromHours(24), "ZkratkyStran",
                    (o) =>
                    {
                        return ZkratkaStranyRepo.ZkratkyVsechStran();
                    });


                Util.Consts.Logger.Info("Static data - UradyObchodujiciSFirmami_s_vazbouNaPolitiky_*");
                UradyObchodujiciSFirmami_s_vazbouNaPolitiky_aktualni_Cache = new Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaUradyStat>
                    (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "UradyObchodujiciSFirmami_s_vazbouNaPolitiky_aktualni",
                    (o) =>
                    {
                        return AnalysisCalculation.UradyObchodujiciSFirmami_s_vazbouNaPolitiky(Relation.AktualnostType.Aktualni, true);
                    }
                    );
                UradyObchodujiciSFirmami_s_vazbouNaPolitiky_nedavne_Cache = new Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaUradyStat>
                    (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "UradyObchodujiciSFirmami_s_vazbouNaPolitiky_nedavne",
                    (o) =>
                    {
                        return AnalysisCalculation.UradyObchodujiciSFirmami_s_vazbouNaPolitiky(Relation.AktualnostType.Nedavny, true);
                    }
                    );
                UradyObchodujiciSFirmami_s_vazbouNaPolitiky_vsechny_Cache = new Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaUradyStat>
                    (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "UradyObchodujiciSFirmami_s_vazbouNaPolitiky_vsechny",
                    (o) =>
                    {
                        return AnalysisCalculation.UradyObchodujiciSFirmami_s_vazbouNaPolitiky(Relation.AktualnostType.Libovolny, true);
                    }
                    );

                Util.Consts.Logger.Info("Static data - UradyObchodujiciSNespolehlivymiPlatciDPH_Cache*");
                UradyObchodujiciSNespolehlivymiPlatciDPH_Cache = new Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaUradyStat>
                    (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "UradyObchodujiciSNespolehlivymiPlatciDPH",
                    (o) =>
                    {
                        return new AnalysisCalculation.VazbyFiremNaUradyStat(); //refresh from task
                    }
                    );
                NespolehlivyPlatciDPH_obchodySurady_Cache = new Devmasters.Cache.File.FileCache<AnalysisCalculation.VazbyFiremNaUradyStat>
                    (Connectors.Init.WebAppDataPath, TimeSpan.Zero, "NespolehlivyPlatciDPH_obchodySurady",
                    (o) =>
                    {
                        return new AnalysisCalculation.VazbyFiremNaUradyStat(); //refresh from task
                    }
                    );



                //AFERY
                Afery.Add("parlamentnilisty", new Lib.Analysis.TemplatedQuery()
                {
                    Text = "Inzerce na Parlamentních listech",
                    Description = "Které úřady inzerují na serveru Parlamentní listy? Smlouvy s inzercí na médiích Our Media a.s. (vydavatel Parlamentních listů) anebo s mediaplánem přímo pro PL. Částky ze smluv jsou orientační a mohou obsahovat objednávky i na jiná média.",
                    Query = "\"OUR MEDIA\" OR \"Parlamentní listy\" OR \"Parlamentnilisty.cz\" OR ico:28876890 OR \"KrajskeListy.cz\" OR \"Krajské listy\" OR \"Prvnizpravy.cz\" OR icoPrijemce:24214868",
                    UrlTemplate = "/HledatSmlouvy?Q={0}",
                    Links = new Lib.Analysis.TemplatedQuery.AHref[]{
                        new Lib.Analysis.TemplatedQuery.AHref("https://web.archive.org/web/20170102215202/http://mediahub.cz/komunikace-35809/clanky-v-parlamentnich-listech-si-plati-ministri-i-hejtmani-celkove-castky-jdou-do-milionu-inzerce-vypada-jako-redakcni-clanky-1058528",
                        "Články v Parlamentních listech si platí ministři i hejtmani. Celkové částky jdou do milionů. Inzerce vypadá jako redakční články"),
                        new Lib.Analysis.TemplatedQuery.AHref("https://denikn.cz/39674/valentuv-server-zautocil-na-transparency-hned-pote-co-ho-narkla-ze-stretu-zajmu-nahoda-rika-senator/",
                        "Valentův server zaútočil na Transparency hned poté, co ho nařkla ze střetu zájmů.")
                    }
                });
                Afery.Add("uklid-praha-cssd", new Lib.Analysis.TemplatedQuery()
                {
                    Text = "Úklidové služby pro firmy členů ČSSD",
                    Description = "Zakázky pro firmy Premio Invest a Lasesmed, které vlastní členové ČSSD a roky dostávaly stovky milionů za úklid v Praze od městských organizací, kde mají vliv sociálnědemokratičtí politici.",
                    Query = "ico:26746590 OR ico:28363809",
                    UrlTemplate = "/HledatSmlouvy?Q={0}",
                    Links = new Lib.Analysis.TemplatedQuery.AHref[]{
                        new Lib.Analysis.TemplatedQuery.AHref("https://zpravy.aktualne.cz/domaci/uklid-prahy-jako-stranicky-byznys-clenove-cssd-vlastni-firmy/r~328647c27c2811e7954a002590604f2e/","Úklid Prahy jako byznys ČSSD. Její členové mají firmy, které žijí ze stamilionových zakázek od města"),
                    }
                });
                Afery.Add("eet", new Lib.Analysis.TemplatedQuery()
                {
                    Text = "EET",
                    Description = "Smlouvy pokrývající vývoj a provoz EET. ",
                    Query = "(ico:03630919 OR ico:72054506 OR ico:72080043) AND (EET OR ADIS)",
                    UrlTemplate = "/HledatSmlouvy?Q={0}",
                    Links = new Lib.Analysis.TemplatedQuery.AHref[]{
                        new Lib.Analysis.TemplatedQuery.AHref("https://dotyk.denik.cz/publicistika/eet-babisuv-nepruhledny-system-na-vymahani-dani-20160915.html","EET: Babišův neprůhledný systém na vymáhání daní"),
                        new Lib.Analysis.TemplatedQuery.AHref("https://www.hlidacstatu.cz/texty/10x-predrazene-eet-skutecne-naklady-na-eet/","10x předražené EET: skutečné náklady na EET"),
                    }
                });
                Afery.Add("elektronicke-myto", new Lib.Analysis.TemplatedQuery()
                {
                    Text = "Elektronické mýto",
                    Description = @"Smlouvy související elektronickým mýtem.",
                    Query = "\"elektronické mýto\"",
                    UrlTemplate = "/HledatSmlouvy?Q={0}"
                });
                Afery.Add("rsd-s-omezenou-soutezi", new Lib.Analysis.TemplatedQuery()
                {
                    Text = "Smlouvy ŘSD s omezenou soutěží",
                    Description = @"Smlouvy ŘSD uzavřené v užším řízení či v jednacím řízení bez uveřejnění.",
                    Query = "icoPlatce:65993390 AND ( \"stavební práce v užším řízení\" OR \"jednací řízení bez uveřejnění\") ",
                    UrlTemplate = "/HledatSmlouvy?Q={0}"
                });

                // hierarchie uradu

                OrganizacniStrukturyUradu = new Devmasters.Cache.File.FileCache<Dictionary<string, List<JednotkaOrganizacni>>>(
                    Connectors.Init.WebAppDataPath, TimeSpan.FromDays(90), "OrganizacniStrukturyUradu", (obj) =>
                    {
                        var _organizaniStrukturyUradu = new Dictionary<string, List<JednotkaOrganizacni>>();
                        try
                        {
                            string path = $"{Connectors.Init.WebAppDataPath}\\ISoSS_Opendata_OSYS_OSSS.xml";

                            var ser = new XmlSerializer(typeof(organizacni_struktura_sluzebnich_uradu));
                            organizacni_struktura_sluzebnich_uradu ossu;
                            using (var streamReader = new StreamReader(path))
                            {
                                using (var reader = XmlReader.Create(streamReader))
                                {
                                    ossu = (organizacni_struktura_sluzebnich_uradu)ser.Deserialize(reader);
                                }
                            }

                            foreach (var urad in ossu.UradSluzebniSeznam.SluzebniUrady)
                            {
                                var f = FirmaRepo.FromDS(urad.idDS);
                                if (f is null || !f.Valid)
                                {
                                    if (string.IsNullOrEmpty(urad.idNadrizene))
                                    {
                                        Util.Consts.Logger.Error($"Organizační struktura - nenalezena datová schránka [{urad.idDS}] úřadu [{urad.oznaceni}]");
                                        continue;
                                    }

                                    var nadrizeny = ossu.UradSluzebniSeznam.SluzebniUrady
                                        .Where(u => u.id == urad.idNadrizene)
                                        .FirstOrDefault();

                                    if (nadrizeny is null)
                                    {
                                        Util.Consts.Logger.Error($"Nenalezen nadřízený úřad, ani datová schránka [{urad.idDS}] úřadu [{urad.oznaceni}]");
                                        continue;
                                    }

                                    f = FirmaRepo.FromDS(nadrizeny.idDS);
                                    if (f is null || !f.Valid)
                                    {
                                        Util.Consts.Logger.Error($"Organizační struktura - nenalezena datová schránka [{nadrizeny.idDS}] nadřízeného úřadu [{nadrizeny.oznaceni}]");
                                        continue;
                                    }
                                }

                                var sluzebniUrad = ossu.OrganizacniStruktura.Where(os => os.id == urad.id)
                                    .FirstOrDefault()
                                    ?.StrukturaOrganizacni?.HlavniOrganizacniJednotka;

                                if (sluzebniUrad is null)
                                {
                                    Util.Consts.Logger.Info($"Služební úřad [{urad.oznaceni}] nemá podřízené organizace.");
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
                            Util.Consts.Logger.Error($"Něco je špatně. Chyba při zpracování struktury úřadů. {ex}");
                        }
                        return _organizaniStrukturyUradu;
                    }, null);


                initialized = true;
            } //lock
            Util.Consts.Logger.Info("Static data - Init DONE");



        }


    }
}