using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.OrgStrukturyStatu;
using HlidacStatu.Repositories.Analysis;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using HlidacStatu.Caching;
using HlidacStatu.Entities.Facts;
using HlidacStatu.Extensions;
using ZiggyCreatures.Caching.Fusion;

namespace HlidacStatu.Repositories
{
    public static class StaticData
    {
        private static readonly ILogger _logger = Log.ForContext(typeof(StaticData));
        
        private static readonly IFusionCache _memoryCache =
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.L1Default, nameof(StaticData));

        public static ValueTask<List<double>> GetBasicStatisticDataAsync() =>
            _memoryCache.GetOrSetAsync($"_BasicStaticData", async _ =>
                {
                    List<double> pol = new List<double>();
                    try
                    {
                        var res = await SmlouvaRepo.Searching.RawSearchAsync("", 1, 0, platnyZaznam: true,
                            anyAggregation:
                            new Nest.AggregationContainerDescriptor<Smlouva>()
                                .Sum("totalPrice", m => m
                                    .Field(ff => ff.CalculatedPriceWithVATinCZK)
                                ), exactNumOfResults: true
                        );
                        var resNepl = await SmlouvaRepo.Searching.RawSearchAsync("", 1, 0, platnyZaznam: false,
                            anyAggregation:
                            new Nest.AggregationContainerDescriptor<Smlouva>()
                                .Sum("totalPrice", m => m
                                    .Field(ff => ff.CalculatedPriceWithVATinCZK)
                                ), exactNumOfResults: true
                        );

                        long platnych = res.Total;
                        long neplatnych = resNepl.Total;
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
                },
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(6)));

        static bool initialized = false;

        public static string Dumps_Path = null;

        public static Devmasters.Cache.AWS_S3.AutoUpdatebleCache<OrganizacniStrukturyUradu>
            OrganizacniStrukturyUraduCache = null;

        public static Devmasters.Cache.LocalMemory.AutoUpdatedCache<List<Sponzoring>> SponzorujiciFirmy_Vsechny = null;
        public static Devmasters.Cache.LocalMemory.AutoUpdatedCache<List<Sponzoring>> SponzorujiciFirmy_Nedavne = null;

        public static Devmasters.Cache.LocalMemory.Cache<Dictionary<string, NespolehlivyPlatceDPH>>
            NespolehlivyPlatciDPH = null;

        public static Devmasters.Cache.LocalMemory.Cache<Darujme.Stats> DarujmeStats = null;
        public static Devmasters.Cache.LocalMemory.Cache<Dictionary<string, string>> ZkratkyStran_cache = null;

        public static Dictionary<string, TemplatedQuery> Afery = new Dictionary<string, TemplatedQuery>();

        static StaticData()
        {
            Init();
        }


        public static void Init()
        {
            if (initialized)
                return;

            string appDataPath = Connectors.Init.WebAppDataPath;
            Devmasters.DT.StopWatchLaps swl = new Devmasters.DT.StopWatchLaps();


            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) +
                                            " var init start");

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


            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) +
                                            " var NespolehlivyPlatciDPH");

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
            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) +
                                            " var Insolvence_firem_politiku");


            _logger.Information("Static data - SponzorujiciFirmy_Vsechny ");

            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) +
                                            " var SponzorujiciFirmy_Vsechny");

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
            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) +
                                            " var SponzorujiciFirmy_nedavne");
            SponzorujiciFirmy_Nedavne = new Devmasters.Cache.LocalMemory.AutoUpdatedCache<List<Sponzoring>>(
                TimeSpan.FromHours(3), (obj) =>
                {
                    return SponzorujiciFirmy_Vsechny.Get()
                        .Where(m =>
                            (m.DarovanoDne.HasValue &&
                             m.DarovanoDne.Value.Add(Relation.NedavnyVztahDelka) > DateTime.Now)
                            ||
                            (m.DarovanoDne.HasValue &&
                             m.DarovanoDne.Value.Add(Relation.NedavnyVztahDelka) > DateTime.Now)
                        )
                        .ToList();
                }
            );

            //if (!System.Diagnostics.Debugger.IsAttached)
            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) +
                                            " var PolitickyAktivni force load");
            OsobaRepo.PolitickyAktivni.Get(); //force to load
            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) +
                                            " var SponzorujiciFirmy_Vsechny force load");
            SponzorujiciFirmy_Vsechny.Get(); //force to load

            _logger.Information("Static data - DarujmeStats");

            swl.StopPreviousAndStartNextLap(Util.DebugUtil.GetClassAndMethodName(MethodBase.GetCurrentMethod()) +
                                            " var DarujmeStats");
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
                        using (Devmasters.Net.HttpClient.URLContent url =
                               new Devmasters.Net.HttpClient.URLContent(
                                   "https://www.darujme.cz/api/v1/project/1200384/stats?apiId=74233883&apiSecret=q2vqimypo2ohpa0qi6g9zwn37rb1bpaan12gulqk"))
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

            //migrace: tohle by mělo jít odsud do Repo cache
            ZkratkyStran_cache = new Devmasters.Cache.LocalMemory.Cache<Dictionary<string, string>>
            (TimeSpan.FromHours(24), "ZkratkyStran",
                (o) => { return ZkratkaStranyRepo.ZkratkyVsechStran(); });


            //AFERY
            Afery.Add("parlamentnilisty", new TemplatedQuery()
            {
                Text = "Inzerce na Parlamentních listech",
                Description =
                    "Které úřady inzerují na serveru Parlamentní listy? Smlouvy s inzercí na médiích Our Media a.s. (vydavatel Parlamentních listů) anebo s mediaplánem přímo pro PL. Částky ze smluv jsou orientační a mohou obsahovat objednávky i na jiná média.",
                Query =
                    "\"OUR MEDIA\" OR \"Parlamentní listy\" OR \"Parlamentnilisty.cz\" OR ico:28876890 OR \"KrajskeListy.cz\" OR \"Krajské listy\" OR \"Prvnizpravy.cz\" OR icoPrijemce:24214868",
                UrlTemplate = "/HledatSmlouvy?Q={0}",
                Links = new TemplatedQuery.AHref[]
                {
                    new TemplatedQuery.AHref(
                        "https://web.archive.org/web/20170102215202/http://mediahub.cz/komunikace-35809/clanky-v-parlamentnich-listech-si-plati-ministri-i-hejtmani-celkove-castky-jdou-do-milionu-inzerce-vypada-jako-redakcni-clanky-1058528",
                        "Články v Parlamentních listech si platí ministři i hejtmani. Celkové částky jdou do milionů. Inzerce vypadá jako redakční články"),
                    new TemplatedQuery.AHref(
                        "https://denikn.cz/39674/valentuv-server-zautocil-na-transparency-hned-pote-co-ho-narkla-ze-stretu-zajmu-nahoda-rika-senator/",
                        "Valentův server zaútočil na Transparency hned poté, co ho nařkla ze střetu zájmů.")
                }
            });
            Afery.Add("uklid-praha-cssd", new TemplatedQuery()
            {
                Text = "Úklidové služby pro firmy členů ČSSD",
                Description =
                    "Zakázky pro firmy Premio Invest a Lasesmed, které vlastní členové ČSSD a roky dostávaly stovky milionů za úklid v Praze od městských organizací, kde mají vliv sociálnědemokratičtí politici.",
                Query = "ico:26746590 OR ico:28363809",
                UrlTemplate = "/HledatSmlouvy?Q={0}",
                Links = new TemplatedQuery.AHref[]
                {
                    new TemplatedQuery.AHref(
                        "https://zpravy.aktualne.cz/domaci/uklid-prahy-jako-stranicky-byznys-clenove-cssd-vlastni-firmy/r~328647c27c2811e7954a002590604f2e/",
                        "Úklid Prahy jako byznys ČSSD. Její členové mají firmy, které žijí ze stamilionových zakázek od města"),
                }
            });
            Afery.Add("eet", new TemplatedQuery()
            {
                Text = "EET",
                Description = "Smlouvy pokrývající vývoj a provoz EET. ",
                Query = "(ico:03630919 OR ico:72054506 OR ico:72080043) AND (EET OR ADIS)",
                UrlTemplate = "/HledatSmlouvy?Q={0}",
                Links = new TemplatedQuery.AHref[]
                {
                    new TemplatedQuery.AHref(
                        "https://dotyk.denik.cz/publicistika/eet-babisuv-nepruhledny-system-na-vymahani-dani-20160915.html",
                        "EET: Babišův neprůhledný systém na vymáhání daní"),
                    new TemplatedQuery.AHref(
                        "https://www.hlidacstatu.cz/texty/10x-predrazene-eet-skutecne-naklady-na-eet/",
                        "10x předražené EET: skutečné náklady na EET"),
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
                Query =
                    "icoPlatce:65993390 AND ( \"stavební práce v užším řízení\" OR \"jednací řízení bez uveřejnění\") ",
                UrlTemplate = "/HledatSmlouvy?Q={0}"
            });
            Afery.Add("clenove-ictunie", new TemplatedQuery()
            {
                Text = "Smlouvy členů ICT Unie",
                Description = @"Smlouvy uzavřené členy ICT Unie a jejich dcerami.",
                Query =
                    "holding:00174939 OR holding:00533394 OR holding:00672416 OR holding:03136108 OR holding:04308697 OR holding:07366949 OR holding:09702652 OR holding:14482282 OR holding:14890992 OR holding:16188781 OR holding:17007909 OR holding:24738875 OR holding:24775487 OR holding:25146441 OR holding:25618067 OR holding:25625632 OR holding:25648101 OR holding:25788001 OR holding:26129558 OR holding:26298953 OR holding:26426331 OR holding:26482347 OR holding:26500281 OR holding:26851652 OR holding:27074358 OR holding:27367061 OR holding:27604977 OR holding:28606582 OR holding:40527514 OR holding:40766314 OR holding:43871020 OR holding:44797320 OR holding:44846029 OR holding:44851391 OR holding:45272808 OR holding:45786259 OR holding:47117087 OR holding:47123737 OR holding:47451084 OR holding:47903783 OR holding:48591254 OR holding:60193336 OR holding:61060631 OR holding:61498084 OR holding:61498483 OR holding:61855057 OR holding:62028235 OR holding:62911643 OR holding:63078236 OR holding:63911035 OR holding:63979462 OR holding:64949541",
                UrlTemplate = "/Hledat?Q={0}"
            });
            Afery.Add("ictunie", new TemplatedQuery()
            {
                Text = "Smlouvy členů ICT Unie",
                Description = @"Smlouvy uzavřené členy ICT Unie a jejich dcerami.",
                Query =
                    "holding:00174939 OR holding:00533394 OR holding:00672416 OR holding:03136108 OR holding:04308697 OR holding:07366949 OR holding:09702652 OR holding:14482282 OR holding:14890992 OR holding:16188781 OR holding:17007909 OR holding:24738875 OR holding:24775487 OR holding:25146441 OR holding:25618067 OR holding:25625632 OR holding:25648101 OR holding:25788001 OR holding:26129558 OR holding:26298953 OR holding:26426331 OR holding:26482347 OR holding:26500281 OR holding:26851652 OR holding:27074358 OR holding:27367061 OR holding:27604977 OR holding:28606582 OR holding:40527514 OR holding:40766314 OR holding:43871020 OR holding:44797320 OR holding:44846029 OR holding:44851391 OR holding:45272808 OR holding:45786259 OR holding:47117087 OR holding:47123737 OR holding:47451084 OR holding:47903783 OR holding:48591254 OR holding:60193336 OR holding:61060631 OR holding:61498084 OR holding:61498483 OR holding:61855057 OR holding:62028235 OR holding:62911643 OR holding:63078236 OR holding:63911035 OR holding:63979462 OR holding:64949541",
                UrlTemplate = "/Hledat?Q={0}"
            });


            // hierarchie uradu
            try
            {
                OrganizacniStrukturyUraduCache =
                    new Devmasters.Cache.AWS_S3.AutoUpdatebleCache<OrganizacniStrukturyUradu>(
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
                                            _logger.Error(
                                                $"Organizační struktura - nenalezena datová schránka [{urad.idDS}] úřadu [{urad.oznaceni}]");
                                            continue;
                                        }

                                        var nadrizeny = ossu.UradSluzebniSeznam.SluzebniUrady
                                            .Where(u => u.id == urad.idNadrizene)
                                            .FirstOrDefault();

                                        if (nadrizeny is null)
                                        {
                                            _logger.Error(
                                                $"Nenalezen nadřízený úřad, ani datová schránka [{urad.idDS}] úřadu [{urad.oznaceni}]");
                                            continue;
                                        }

                                        f = FirmaRepo.FromDS(nadrizeny.idDS);
                                        if (f is null || !f.Valid)
                                        {
                                            _logger.Error(
                                                $"Organizační struktura - nenalezena datová schránka [{nadrizeny.idDS}] nadřízeného úřadu [{nadrizeny.oznaceni}]");
                                            continue;
                                        }
                                    }

                                    var sluzebniUrad = ossu.OrganizacniStruktura.Where(os => os.id == urad.id)
                                        .FirstOrDefault()
                                        ?.StrukturaOrganizacni?.HlavniOrganizacniJednotka;

                                    if (sluzebniUrad is null)
                                    {
                                        _logger.Information(
                                            $"Služební úřad [{urad.oznaceni}] nemá podřízené organizace.");
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

            swl.StopAll();
            _logger.Warning("StaticData times " + swl.ToString());
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