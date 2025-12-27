using ExcelDataReader;
using HlidacStatu.Caching;
using HlidacStatu.Connectors;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Util;
using InfluxDB.Client.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Nest;
using Polly;
using Polly.Extensions.Http;
using Polly.Retry;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using ZiggyCreatures.Caching.Fusion;
using static HlidacStatu.Entities.Firma;

namespace HlidacStatu.Repositories
{
    public static class FirmaVlastnenaStatemRepo
    {
        private static ILogger _logger = Serilog.Log.Logger.ForContext(typeof(FirmaVlastnenaStatemRepo));

        //vcetne strategickych podniku z https://www.mfcr.cz/cs/aktualne/tiskove-zpravy/2020/vlada-schvalila-strategii-vlastnicke-pol-37573
        public static string[] StatniFirmyICO = new string[]
        {
            "00014818", // ARMABETON, a.s., v likvidaci
            "00003026", // Automatizace železniční dopravy Praha, státní podnik "v likvidaci"
            "25125877", // BALMED Praha, státní podnik
            "00012033", // BENZINA, státní podnik"v likvidaci
            "00015296", // Brněnské cihelny, státní podnik - v likvidaci
            "00012343", // Brněnské papírny, státní podnik - v likvidaci
            "00514152", // Budějovický Budvar, národní podnik, Budweiser Budvar, National Corporation, Budweiser Budvar, Entreprise Nationale
            "00311391", // CENDIS, s.p.
            "00013455", // CENTRUM - F, a.s. v likvidaci
            "27892646", // Centrum Holešovice a.s.
            "00015270", // Cihlářské a keramické závody Teplice, státní podnik v likvidaci
            "25085531", // CSA Services, s.r.o.
            "25674285", // Czech Airlines Handling, a.s.
            "27145573", // Czech Airlines Technics, a.s.
            "24829871", // ČD - Informační Systémy, a.s.
            "61459445", // ČD - Telematika a.s.
            "28196678", // ČD Cargo, a.s.
            "27195872", // ČD Reality a.s.
            "27364976", // ČD travel, s.r.o.
            "60193531", // ČEPRO, a.s.
            "25702556", // ČEPS, a.s.
            "63078333", // Česká exportní banka, a.s.
            "27772683", // Česká pošta Security, s.r.o.
            "47114983", // Česká pošta, s.p.
            "45795908", // České aerolinie a.s.
            "70994226", // České dráhy, a.s.
            "00002691", // České energetické závody, státní podnik, odštěpný závod Elektrárna Tisová Zkratka : ČEZ-ETI - výmaz k 20.10.1992
            "26162539", // Českomoravská společnost chovatelů, a.s.
            "14888025", // Československá automobilová doprava, Ústřední autobusové nádraží, st.podnik "v likvidaci"
            "00086932", // Československá státní automobilová doprava, státní podnik Hradec Králové
            "24821993", // Český Aeroholding, a.s.
            "24729035", // ČEZ Distribuce, a. s.
            "26871823", // ČEZ Distribuční služby, s.r.o.
            "28255933", // ČEZ Energetické produkty, s.r.o.
            "60698101", // ČEZ ENERGOSERVIS spol. s r.o.
            "27804721", // ČEZ ESL, s.r.o.
            "26470411", // ČEZ ICT Services, a. s.
            "28861736", // ČEZ Invest Slovensko, a.s.
            "26206803", // ČEZ Korporátní služby, s.r.o.
            "26840065", // ČEZ Logistika, s.r.o.
            "25938924", // ČEZ Obnovitelné zdroje, s.r.o.
            "27232433", // ČEZ Prodej, a.s.
            "27309941", // ČEZ Teplárenská, a.s.
            "26376547", // ČEZ Zákaznické služby, s.r.o.
            "45274649", // ČEZ, a. s.
            "00565253", // ČKD DOPRAVNÍ SYSTÉMY,a.s. v likvidaci
            "00011011", // ČKD DUKLA a.s.
            "00002674", // ČPPT, s.p.
            "00002739", // DIAMO, státní podnik
            "27378225", // Dopravní vzdělávací institut, a.s.
            "27786331", // DPOV, a.s.
            "49241672", // Editio Praga a.s. v likvidaci
            "28786009", // Elektrárna Chvaletice a.s.
            "00001481", // Elektrotechnický zkušební ústav, s. p.
            "26051818", // Energetické centrum s.r.o.
            "47115726", // Energotrans, a.s.
            "25291581", // Explosia a.s.
            "45279314", // Exportní garanční a pojišťovací společnost, a.s.
            "00577880", // Fyzikálně technický zkušební ústav, státní podnik
            "26175291", // GALILEO REAL, k.s. v likvidaci
            "44269595", // Harvardský průmyslový holding, a.s.
            "45274827", // HEXA PLUS, a.s. v likvidaci
            "45144419", // HOLDING KLADNO a.s."v likvidaci"
            "61860336", // HOLIDAYS Czech Airlines, a.s.
            "25634160", // HOLTE MEDICAL, a.s. v likvidaci
            "25059386", // HOLTE s.r.o. - v likvidaci
            "14450216", // Horské lázně Karlova Studánka, státní podnik
            "00251976", // Hotelinvest a.s.
            "48535591", // IKEM - služby, spol. s r.o., v likvidaci
            "60197901", // IMOB a.s. v likvidaci
            "00014079", // Jihomoravské dřevařské závody Brno, v likvidaci
            "49973720", // Jihomoravské pivovary, a.s. v likvidaci
            "63080249", // Kongresové centrum Praha, a.s.
            "25255843", // KORADO, a.s.
            "42196451", // Lesy České republiky, s.p.
            "28244532", // Letiště Praha, a. s.
            "14867770", // LINETA Severočeská dřevařská společnost a.s.
            "00000515", // LOM PRAHA s.p.
            "47673354", // MCI HOLDING a.s. - v likvidaci
            "60193468", // MERO ČR, a.s.
            "43833560", // Mezinárodní testování drůbeže, státní podnik
            "15503852", // Moravskoslezské dřevařské závody, Šumperk a.s. "v likvidaci"
            "00009334", // Mototechna, státní podnik "v likvidaci"
            "60196696", // MUFIS a.s.
            "47677543", // NAKIT
            "44848943", // Národní rozvojová banka, a.s.
            "00000345", // NAŠE VOJSKO, TISKÁRNA, PRAHA, státní podnik
            "00009181", // ORLIČAN, a.s., v likvidaci
            "00009563", // OSAN, obchodní podnik v likvidaci
            "26463318", // OTE, a.s.
            "00007536", // Palivový kombinát Ústí, státní podnik
            "49241494", // Podpůrný a garanční rolnický a lesnický fond, a.s.
            "45273448", // Poštovní tiskárna cenin Praha s.r.o.
            "70890005", // Povodí Labe, státní podnik
            "70890013", // Povodí Moravy, s.p.
            "70890021", // Povodí Odry, státní podnik
            "70889988", // Povodí Ohře, státní podnik
            "70889953", // Povodí Vltavy, státní podnik
            "46504818", // Prefa Pardubice a.s.
            "00157287", // PRIOR - Severočeské obchodní domy, státní podnik v "likvidaci"
            "00157325", // PRIOR - Severomoravské OD Ostrava v likvidaci, státní podnik
            "46355901", // PRISKO a.s.
            "17047234", // RAILREKLAM, spol. s r.o.
            "00664073", // RKT - Rovnací a kotevní technika, státní podnik v likvidaci
            "49823574", // Robin Oil s.r.o.
            "49710371", // Řízení letového provozu České republiky, státní podnik (ŘLP ČR, s.p.)
            "46708707", // SETUZA a.s.
            "49901982", // Severočeské doly a.s.
            "00015156", // Severočeské keramické závody Most, státní podnik v likvidaci
            "48291749", // Severočeské mlékárny, a.s. Teplice
            "00015415", // Severokámen Liberec, státní podnik v likvidaci
            "00076791", // Silnice, státní podnik Plzeň - v likvidaci
            "49453866", // Slovácké vodárny a kanalizace, a. s.
            "62413376", // Správa Letiště Praha, s.p.
            "70994234", // Správa železnic, státní organizace
            "14450241", // Státní léčebné lázně Bludov, státní podnik "v likvidaci"
            "00024007", // Státní léčebné lázně Janské Lázně, státní podnik
            "03630919", // Státní pokladna Centrum sdílených služeb, s. p.
            "00001279", // Státní tiskárna cenin, s. p.
            "27146235", // Státní zkušebna strojů a.s.
            "00001490", // Strojírenský zkušební ústav, s.p.
            "00008141", // Středočeské energetické závody, státní podnik "v likvidaci"
            "00128201", // ŠKODA PRAHA a.s.
            "27257517", // ŠKODA PRAHA Invest s.r.o.
            "00015679", // Technický a zkušební ústav stavební Praha, s.p.
            "00002321", // TECHNOMAT, státní podnik "v likvidaci"
            "28707052", // Teplárna Trmice, a.s.
            "00013251", // Textilní zkušební ústav, s.p.
            "25401726", // THERMAL-F, a.s.
            "45534268", // TRANSPORTA a.s.
            "28267141", // TRANZA Strojírny a.s.
            "45147965", // Ústav nerostných surovin a. s. v likvidaci
            "45193070", // VÍTKOVICE, a.s.
            "49454561", // Vodovody a kanalizace Zlín, a.s.
            "00000205", // Vojenské lesy a statky ČR, s.p.
            "00000337", // Vojenské stavby - státní podnik v likvidaci
            "00659819", // Vojenský opravárenský podnik 081 Přelouč, státní podnik
            "24272523", // Vojenský technický ústav, s.p.
            "29372259", // Vojenský výzkumný ústav, s. p.
            "00000493", // VOP CZ, s.p.
            "45273375", // VSZP, a.s. v likvidaci
            "00000370", // Vydavatelství MAGNET-PRESS
            "00008184", // Východočeské energetické závody, státní podnik v likvidaci
            "00014125", // Výzkumný a vývojový ústav dřevařský, Praha, s.p.
            "27257258", // Výzkumný Ústav Železniční, a.s.
            "00010669", // VZLU AEROSPACE, a.s.
            "48204285", // Zemský hřebčinec Písek státní podnik v likvidaci
            "13695673", // Zemský hřebčinec Tlumačov, státní podnik v likvidaci
            "00009393", // ZPS, a.s. v likvidaci

        };

        public static int[] StatniFirmy_BasedKodPF = new int[]
        {
            301, 302, 312, 313, 314, 325, 331, 352, 353, 361, 362, 381, 382, 391, 392, 521,525, 771, 801, 804, 805,811,

        };

        /*
         * https://wwwinfo.mfcr.cz/ares/aresPrFor.html.cz
         * KOD_PF
         *
301	Státní podnik
302	Národní podnik
312	Banka-státní peněžní ústav
313	Česká národní banka
314	Česká konsolidační agentura
325	Organizační složka státu
331	Příspěvková organizace
352	Správa železniční dopravní cesty, státní organizace
353	Rada pro veřejný dohled nad auditem
361	Veřejnoprávní instituce (ČT,ČRo,ČTK)
362	Česká tisková kancelář
381	Fond (ze zákona)
382	Státní fond ze zákona

521	Samostatná drobná provozovna obecního úřadu
771	Svazek obcí
801	Obec nebo městská část hlavního města Prahy
804	Kraj
805	Regionální rada regionu soudržnosti

 */






        public static bool MuzeBytStatni_PodlePravniFormaKod(int? kod)
        {
            if (kod.HasValue == false)
                return true;

            var okPF = true;
            okPF = okPF && kod > 110;
            okPF = okPF && kod != 422;
            okPF = okPF && kod != 423;
            okPF = okPF && kod != 424;
            okPF = okPF && kod != 425;
            okPF = okPF && !(kod >= 700 && kod < 750);
            okPF = okPF && kod != 901;
            okPF = okPF && kod != 907;
            okPF = okPF && kod != 908;
            okPF = okPF && kod != 911;
            okPF = okPF && kod != 921;
            okPF = okPF && kod != 937;
            okPF = okPF && kod != 938;
            okPF = okPF && kod != 999;
            return okPF;
        }

        private static IFusionCache PermanentCache =>
            HlidacStatu.Caching.CacheFactory.CreateNew(CacheFactory.CacheType.PermanentStore, nameof(FirmaVlastnenaStatemRepo));



        public async static Task<HashSet<string>> IcaStatnichFiremAsync()
        {
            using (DbEntities db = new DbEntities())
            {
                var q = db.Firma
                    .AsNoTracking()
                    .Where(Firma.StatniFirmaPredicate)
                    .Select(m => m.ICO)
                    .AsQueryable();
                var r = await q.ToHashSetAsync();
                return r;
            }
        }

        public async static Task<HashSet<string>> IcaUraduStatnichFiremAsync()
        {
            using (DbEntities db = new DbEntities())
            {
                var q = db.Firma
                    .AsNoTracking()
                    .Where(Firma.PatrimStatuPredicate)
                    .Select(m => m.ICO)
                    .AsQueryable();
                var r = await q.ToHashSetAsync();
                return r;
            }
        }

        public async static Task<HashSet<string>> IcaOVMAsync()
        {
            using (DbEntities db = new DbEntities())
            {
                var q = db.Firma
                    .AsNoTracking()
                    .Where(Firma.OVMSubjektPredicate)
                    .Select(m => m.ICO)
                    .AsQueryable();
                var r = await q.ToHashSetAsync();
                return r;
            }
        }

        static HashSet<string> _ovm_Nikdy_nejsou_statni = null;
        private static async Task<HashSet<string>> OVM_Nikdy_nejsou_statni(bool updateFromPrimarySource = false)
        {
            if (updateFromPrimarySource)
                await Update_OrganVerejneMoci_in_DBAsync();

            if (updateFromPrimarySource ||  _ovm_Nikdy_nejsou_statni == null)
            {
                using DbEntities db = new DbEntities();

                //kategorie, ktere nikdy nejsou statni
                //OVM z OrganVerejneMoci
                //ale ne ty, co maji kategorii OVM ze seznamu nize (seznam kategorii ovm https://rpp-ais.egon.gov.cz/AISP/verejne/ovm-spuu/katalog-kategorii-ovm)
                var forbiddenOVMKategories = new HashSet<string>()
            {
                "KO118", //Soudní exekutoři
                "KO155", //Notáři
                "KO505", //Soukromé vysoké školy
                "KO520", //Profesní komory v agendě ochrany spotřebitele
                "KO537", //Insolvenční správci - fyzická osoba
                "KO538", //Insolvenční správci - v.o.s.
                "KO542", //Soukromé archivy
            };
                _logger.Information("Loading OVM, kteri nikdy nemohou byt statni firmou...");
                var res1 = await db.OrganVerejneMoci
                    .AsNoTracking()
                    .Join(
                        db.OrganVerejneMoci_KategorieOvm.AsNoTracking(),
                        o => o.IdDS,
                        k => k.IdDS,
                        (o, k) => new { o, k }
                    )
                    .Where(m => m.o.ICO != null)
                    .Where(m => 
                        forbiddenOVMKategories.Contains(m.k.KategorieOvm)
                    )
                    .Select(m => m.o.ICO)
                    .Distinct()
                    .ToHashSetAsync();
                var res2 = await db.OrganVerejneMoci
                    .AsNoTracking()
                    .Where(m => m.ICO != null)
                    .Where(m => m.TypDS.Contains("PFO") //fyzicke osoby nikdy nemohou byt statni firmou
                        )
                    .Select(m => m.ICO)
                    .Distinct()
                    .ToHashSetAsync();

                _ovm_Nikdy_nejsou_statni = res1.Union(res2).ToHashSet();

                //rucne pridane ICO, ktere nikdy nemohou byt statni firmou
                _ovm_Nikdy_nejsou_statni.Add("25755277"); //Clovek v tisni
                _ovm_Nikdy_nejsou_statni.Add("00001350"); //csob
                _ovm_Nikdy_nejsou_statni.Add("24288110"); //Elektrárna Počerady, a.s.
                _ovm_Nikdy_nejsou_statni.Add("28786009"); //Elektrárna Chvaletice a.s.

            }
            return _ovm_Nikdy_nejsou_statni;
            ;
        }

        //merge of Firmy.GlobalStatistics.VsechnyUradyAsync
        //Tasks.Merk.UpdateListStatniFirmy_InDB
        public async static Task Najdi_Firmy_Vlastnene_Statem_z_Primarnich_ZdrojuAsync(
            Action<string> outputWriter = null,
            Action<Devmasters.Batch.ActionProgressData> progressWriter = null
            )
        {
            using DbEntities db = new DbEntities();

            progressWriter = progressWriter ?? new Devmasters.Batch.ActionProgressWriter(0.1f).Writer;

            //updatni OVM v DB
            var updateOVM = true;
            if (System.Diagnostics.Debugger.IsAttached)
                updateOVM = false; //jen pri ladeni
            HashSet<string> ovm_nikdyStatni = await OVM_Nikdy_nejsou_statni(updateOVM); 

            //Find statni firmy 

            List<string> bagOfIco = new List<string>();

            bagOfIco.AddRange(StatniFirmyICO);

            _logger.Information("Adding statni firmy podle pravni formy...");
            //vsechny firmy z OR podle pravni formy
            bagOfIco.AddRange(
                db.Firma.Where(m => StatniFirmy_BasedKodPF.Contains(m.Kod_PF ?? -1))
                .AsNoTracking()
                .Select(m => m.ICO)
                .ToList()
            );

            _logger.Information("Downloading seznamu verejnych spolecnosti a vladnich instituci z mfcr.cz Part 1...");
            //firmy z MFCR - verejne spolecnosti a vladni instituce
            Console.WriteLine("Downloading from mfcr.cz 1");
            string MFLink = "https://www.mfcr.cz/assets/cs/media/Rozp-ramce-zak-23-2017_2021_Seznam-verejnych-spolecnosti-v-CR-rijen-2021.xlsx";
            string fn = @"c:\!\Seznam-verejnych-spolecnosti.xlsx";
            try
            {

                using (var net = new Devmasters.Net.HttpClient.URLContent(MFLink))
                {
                    net.Timeout = 60 * 10 * 1000;
                    var bin = net.GetBinary();
                    System.IO.File.Delete(fn);
                    System.IO.File.WriteAllBytes(fn, bin.Binary);
                }
            }
            catch (Exception e)
            {

                _logger.Error(e, "Chyba pri stahovani seznamu verejnych spolecnosti z " + MFLink);
            }


            _logger.Information("Downloading seznamu verejnych spolecnosti a vladnich instituci z mfcr.cz Part 2...");
            Console.WriteLine("Downloading from mfcr.cz 2");
            string MFLink2 = "https://www.mfcr.cz/assets/cs/media/Rozp-ramce-zak-23-2017_2021_Seznam-vladnich-instituci-v-CR-rijen-2021.xlsx";
            string fn2 = @"c:\!\Seznam-vladnich-spolecnosti.xlsx";
            try
            {
                using (var net = new Devmasters.Net.HttpClient.URLContent(MFLink2))
                {
                    net.Timeout = 60 * 10 * 1000;
                    var bin = net.GetBinary();
                    System.IO.File.Delete(fn2);
                    System.IO.File.WriteAllBytes(fn2, bin.Binary);
                }

            }
            catch (Exception e)
            {

                _logger.Error(e, "Chyba pri stahovani seznamu verejnych spolecnosti z " + MFLink2);
            }

            // .net core support
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            if (System.IO.File.Exists(fn))
            {
                _logger.Information("Parsing downloaded excel files Part 1...");
                Console.WriteLine("parsing excel 1");
                using (var stream = System.IO.File.Open(fn, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                    {
                        var excel = reader.AsDataSet();
                        int count = 0;
                        foreach (System.Data.DataRow dr in excel.Tables[0].Rows)
                        {
                            count++;
                            if (count > 5)
                            {
                                bagOfIco.Add(dr[1].ToString());
                            }
                        }
                    }
                }
            }

            if (System.IO.File.Exists(fn2))
            {
                _logger.Information("Parsing downloaded excel files Part 1...");
                Console.WriteLine("parsing excel 2");
                using (var stream = System.IO.File.Open(fn2, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                    {
                        var excel = reader.AsDataSet();
                        int count = 0;
                        foreach (System.Data.DataRow dr in excel.Tables[0].Rows)
                        {
                            count++;
                            if (count > 5)
                            {
                                bagOfIco.Add(dr[1].ToString());
                            }
                        }
                    }
                }

            }




            _logger.Debug("Cleaning up duplicates ...");
            //delete duplicates and empties
            bagOfIco = bagOfIco
                .Where(m => string.IsNullOrEmpty(m) == false)
                .Distinct()
                .ToList();

            _logger.Information("Adding statni firmy...");
            var allOvm = await db.OrganVerejneMoci.AsNoTracking()
                .Select(m => m.ICO)
                .ToListAsync();
            bagOfIco.AddRange(allOvm);

            _logger.Information("Adding statni firmy podle oboru cinnosti fakultni nemocnice...");
            bagOfIco.AddRange(await FirmaRepo.Zatrideni.GetIcoDirectAsync(Firma.Zatrideni.SubjektyObory.Fakultni_nemocnice));
            _logger.Information("Adding statni firmy podle oboru cinnosti hasice...");
            bagOfIco.AddRange(await FirmaRepo.Zatrideni.GetIcoDirectAsync(Firma.Zatrideni.SubjektyObory.Hasicsky_zachranny_sbor));
            _logger.Information("Adding statni firmy podle oboru cinnosti knihovny...");
            bagOfIco.AddRange(await FirmaRepo.Zatrideni.GetIcoDirectAsync(Firma.Zatrideni.SubjektyObory.Knihovny));
            _logger.Information("Adding statni firmy podle oboru cinnosti statni media...");
            bagOfIco.AddRange(await FirmaRepo.Zatrideni.GetIcoDirectAsync(Firma.Zatrideni.SubjektyObory.StatniMedia));
            _logger.Information("Adding statni firmy podle oboru cinnosti krajske spravy silnic...");
            bagOfIco.AddRange(await FirmaRepo.Zatrideni.GetIcoDirectAsync(Firma.Zatrideni.SubjektyObory.Krajske_spravy_silnic));
            _logger.Information("Adding statni firmy podle oboru cinnosti velke nemocnice...");
            bagOfIco.AddRange(await FirmaRepo.Zatrideni.GetIcoDirectAsync(Firma.Zatrideni.SubjektyObory.Velke_nemocnice));
            _logger.Information("Adding statni firmy podle oboru cinnosti verejne vysoke skoly...");
            bagOfIco.AddRange(await FirmaRepo.Zatrideni.GetIcoDirectAsync(Firma.Zatrideni.SubjektyObory.Verejne_vysoke_skoly));
            _logger.Information("Adding statni firmy podle oboru cinnosti zdravotni pojistovny...");
            bagOfIco.AddRange(await FirmaRepo.Zatrideni.GetIcoDirectAsync(Firma.Zatrideni.SubjektyObory.Zdravotni_pojistovny));


            //smaz vsechny OVM, kteri maji kategorii, ktera znemožnuje byt statni firmou
            //cleanup a filtruj ico, co nikdy nejsou statni
            _logger.Debug("Smaz duplicate a subjekty, co nikdy nejsou statni...");
            bagOfIco = bagOfIco
                .Where(m => string.IsNullOrEmpty(m) == false)
                .Select(m=>m.Trim())
                .Distinct()
                .Where(m => ovm_nikdyStatni.Contains(m) == false)                
                .ToList();

            System.Collections.Concurrent.ConcurrentDictionary<string, string> so_far_found_subjects = new();
            Devmasters.Batch.Manager.DoActionForAll<string>(bagOfIco,
             (ico) =>
             {
                 so_far_found_subjects.TryAdd(ico, Firmy.GetJmeno(ico));
                 return new Devmasters.Batch.ActionOutputData();
             }, outputWriter, progressWriter,true);

            foreach (var kv in so_far_found_subjects.OrderBy(o=>o.Value))
                System.IO.File.AppendAllText(@"c:\!\statni_subjekty_prehled.txt", $"{kv.Key}\t{kv.Value}\r\n");

            //najdi vsechny subjekty a jejich vazby           
            _logger.Information("Najdi vsechny podrizene subjekty ...");
            object lockObj = new object();
            System.Collections.Concurrent.ConcurrentBag<string> foundIcos = new(); 

            Devmasters.Batch.Manager.DoActionForAll<string, object>(bagOfIco,
             (ico, obj) =>
             {
                 var f = Firmy.Get(ico);
                 if (f.Valid && FirmaVlastnenaStatemRepo.MuzeBytStatni_PodlePravniFormaKod(f.Kod_PF))
                 {

                     List<string> subjIco = new List<string>();
                     subjIco.Add(Util.ParseTools.NormalizeIco(f.ICO));
                     var vazby = f.AktualniVazby(Relation.AktualnostType.Aktualni, Relation.CharakterVazbyEnum.VlastnictviKontrola, true);
                     subjIco.AddRange(vazby.Select(m => Util.ParseTools.NormalizeIco(m.To.Id)));
                     foreach (var inIc in subjIco)
                     {
                         var ff = Firmy.Get(inIc);
                         if (ff.Valid && FirmaVlastnenaStatemRepo.MuzeBytStatni_PodlePravniFormaKod(ff.Kod_PF))
                         {
                             lock (lockObj)
                             {
                                 foundIcos.Add(inIc);
                             }
                         }
                     }
                 }

                 return new Devmasters.Batch.ActionOutputData();
             }, null, outputWriter, progressWriter,
             !System.Diagnostics.Debugger.IsAttached, prefix: "freshList statnifirmy ", monitor: new MonitoredTaskRepo.ForBatch()
             );

            bagOfIco.AddRange(foundIcos);

            //cleanup a filtruj ico, co nikdy nejsou statni
            _logger.Debug("Smaz duplicate a subjekty, co nikdy nejsou statni...");
            bagOfIco = bagOfIco
                .Select(m => m.Trim())
                .Where(m => string.IsNullOrEmpty(m) == false)
                .Distinct()
                .Where(m => ovm_nikdyStatni.Contains(m) == false)
                .ToList();


            //update DB with bagOfIco as statni firmy
            //reset typ for all
            _logger.Information("Updating DB ...");
            _logger.Information("Resetting all firmy to Soukromy ...");
            string sql = $"update firma set typ={((int)(TypSubjektuEnum.Soukromy))} where typ != {((int)(TypSubjektuEnum.Soukromy))}"; // where ico not in ({ica})
            DirectDB.Instance.NoResult(sql);

            List<string[]> batch = bagOfIco.Chunk(500).ToList();
            _logger.Information("Setting statni firmy ...");
            Devmasters.Batch.Manager.DoActionForAll<IEnumerable<string>>(batch,
                (icob) =>
                {
                    string icos = string.Join(",", icob.Select(i => $"'{i}'"));
                    string sql = $"update firma set typ={((int)(TypSubjektuEnum.PatrimStatu))} where ico in ({icos})";
                    DirectDB.Instance.NoResult(sql);

                    return new Devmasters.Batch.ActionOutputData();
                }, outputWriter, progressWriter,
                !System.Diagnostics.Debugger.IsAttached, prefix: "UpdateFirmyWithTyp STATNI", monitor: new MonitoredTaskRepo.ForBatch()
            );

            batch.Clear();
            batch = allOvm
                .Where(m => ovm_nikdyStatni.Contains(m) == false)
                .Chunk(500).ToList();
            _logger.Information("Setting OVM typ...");
            Devmasters.Batch.Manager.DoActionForAll<IEnumerable<string>>(batch,
                (icob) =>
                {
                    string icos = string.Join(",", icob.Select(i => $"'{i}'"));
                    string sql = $"update firma set typ={((int)(TypSubjektuEnum.Ovm))} where ico in ({icos})";
                    DirectDB.Instance.NoResult(sql);

                    return new Devmasters.Batch.ActionOutputData();
                }, outputWriter, progressWriter,
                !System.Diagnostics.Debugger.IsAttached, prefix: "UpdateFirmyWithTyp OVM", monitor: new MonitoredTaskRepo.ForBatch()
            );

            //obce
            var sqlObce = @"select distinct ico from OrganVerejneMoci ovm
	inner join OrganVerejneMoci_KategorieOvm k on ovm.IdDS=k.IdDS
	where k.KategorieOvm='KO14' or k.KategorieOvm='KO12' or k.KategorieOvm='KO137'";
            batch.Clear();
            batch = DirectDB.Instance.GetList<string>(sqlObce)
                .Chunk(500).ToList();
            _logger.Information("Setting OBCE typ...");
            Devmasters.Batch.Manager.DoActionForAll<IEnumerable<string>>(batch,
                (icob) =>
                {
                    string icos = string.Join(",", icob.Select(i => $"'{i}'"));
                    string sql = $"update firma set typ={((int)(TypSubjektuEnum.Obec))} where ico in ({icos})";
                    DirectDB.Instance.NoResult(sql);

                    return new Devmasters.Batch.ActionOutputData();
                }, outputWriter, progressWriter,
                !System.Diagnostics.Debugger.IsAttached, prefix: "UpdateFirmyWithTyp OBCE", monitor: new MonitoredTaskRepo.ForBatch()
            );

            //insolvencni spravci
            batch.Clear();
            batch = (await FirmaRepo.Zatrideni.GetIcoDirectAsync(Zatrideni.SubjektyObory.Insolvencni_spravci_fyzicke_osoby))
                    .Union(await FirmaRepo.Zatrideni.GetIcoDirectAsync(Zatrideni.SubjektyObory.Insolvencni_spravci_pravnicke_osoby))
                        .Chunk(500).ToList();
            _logger.Information("Setting INSOLVENCNI SPRAVCI typ...");
            Devmasters.Batch.Manager.DoActionForAll<IEnumerable<string>>(batch,
                (icob) =>
                {
                    string icos = string.Join(",", icob.Select(i => $"'{i}'"));
                    string sql = $"update firma set typ={((int)(TypSubjektuEnum.InsolvecniSpravce))} where ico in ({icos})";
                    DirectDB.Instance.NoResult(sql);

                    return new Devmasters.Batch.ActionOutputData();
                }, outputWriter, progressWriter,
                !System.Diagnostics.Debugger.IsAttached, prefix: "UpdateFirmyWithTyp insolvecni spravci", monitor: new MonitoredTaskRepo.ForBatch()
            );
            //exekutori
            batch.Clear();
            batch = (await FirmaRepo.Zatrideni.GetIcoDirectAsync(Zatrideni.SubjektyObory.Soudni_exekutori))
                        .Chunk(500).ToList();
            _logger.Information("Setting EXEKUTOR typ...");
            Devmasters.Batch.Manager.DoActionForAll<IEnumerable<string>>(batch,
                (icob) =>
                {
                    string icos = string.Join(",", icob.Select(i => $"'{i}'"));
                    string sql = $"update firma set typ={((int)(TypSubjektuEnum.Exekutor))} where ico in ({icos})";
                    DirectDB.Instance.NoResult(sql);

                    return new Devmasters.Batch.ActionOutputData();
                }, outputWriter, progressWriter,
                !System.Diagnostics.Debugger.IsAttached, prefix: "UpdateFirmyWithTyp exekutor", monitor: new MonitoredTaskRepo.ForBatch()
            );

            _logger.Information("Done updating statni firmy.");

        }



        [XmlRoot(ElementName = "SeznamOvmIndex")]
        public class SeznamOvmIndex
        {
            [XmlElement(ElementName = "Subjekt")]
            public List<OrganVerejneMoci> Subjekt { get; set; }
        }

        public static async Task Update_OrganVerejneMoci_in_DBAsync()
        {
            AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(5, _ => TimeSpan.FromSeconds(1));

        _logger.Information("Running OVM download");
            string ovmUrl = "https://www.czechpoint.cz/spravadat/ovm/datafile.do?format=xml&service=seznamovm";
            var httpClient = new HttpClient();

            using var responseMessage = await _retryPolicy.ExecuteAsync(() => httpClient.GetAsync(ovmUrl));

            if (!responseMessage.IsSuccessStatusCode)
            {
                _logger.Error(
                    "Error during {methodName}. Server responded with [{statusCode}] status code. Reason phrase [{reasonPhrase}].",
                    nameof(Update_OrganVerejneMoci_in_DBAsync), responseMessage.StatusCode, responseMessage.ReasonPhrase);
                throw new Exception($"Can't download data from {ovmUrl}");
            }

            var stream = await responseMessage.Content.ReadAsStreamAsync();
            using var gZipStream = new GZipStream(stream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();

            await gZipStream.CopyToAsync(decompressedStream);
            decompressedStream.Position = 0;
            using var text = new XmlTextReader(decompressedStream);
            text.Namespaces = false;

            XmlSerializer serializer = new XmlSerializer(typeof(SeznamOvmIndex));
            var data = (SeznamOvmIndex)serializer.Deserialize(text);

            if (data is null)
            {
                _logger.Error("No data were serialized");
                throw new Exception("No data were serialized.");
            }

            Dictionary<int, TypOvm> typyOvm = new();
            Dictionary<string, PravniFormaOvm> pfOvm = new();
            Dictionary<int, AdresaOvm> adresyOvm = new();

            using var db = new DbEntities();
            var strategy = db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    _logger.Information("Truncating OVM related tables");
                    await db.Database.ExecuteSqlRawAsync("truncate table AdresaOvm");
                    _logger.Information("Truncated AdresaOvm");
                    await db.Database.ExecuteSqlRawAsync("truncate table PravniFormaOvm");
                    _logger.Information("Truncated PravniFormaOvm");
                    await db.Database.ExecuteSqlRawAsync("truncate table TypOvm");
                    _logger.Information("Truncated TypOvm");
                    await db.Database.ExecuteSqlRawAsync("truncate table OrganVerejneMoci");
                    _logger.Information("Truncated OrganVerejneMoci");
                    await db.Database.ExecuteSqlRawAsync("truncate table OrganVerejneMoci_KategorieOvm");
                    _logger.Information("Truncated OrganVerejneMoci_KategorieOvm");

                    _logger.Information("Inserting OVM data into database");
                    long count = 0;
                    foreach (var ovm in data.Subjekt)
                    {
                        count++;
                        // replace with correct unique record
                        if (typyOvm.TryAdd(ovm.TypOvm.Id, ovm.TypOvm) == false)
                            ovm.TypOvm = typyOvm[ovm.TypOvm.Id];

                        if (pfOvm.TryAdd(ovm.PravniFormaOvm.Id, ovm.PravniFormaOvm) == false)
                            ovm.PravniFormaOvm = pfOvm[ovm.PravniFormaOvm.Id];

                        if (adresyOvm.TryAdd(ovm.AdresaOvm.Id, ovm.AdresaOvm) == false)
                            ovm.AdresaOvm = adresyOvm[ovm.AdresaOvm.Id];

                        if (!string.IsNullOrEmpty(ovm.KategorieOvm))
                        {
                            var kategorieOvm = ovm.KategorieOvm.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var kat in kategorieOvm.Select(m => m.Trim()).Distinct())
                            {
                                db.OrganVerejneMoci_KategorieOvm.Add(new OrganVerejneMoci_KategorieOvm()
                                {
                                    KategorieOvm = kat,
                                    IdDS = ovm.IdDS
                                });
                            }
                        }
                        if (count % 50 == 0)
                            _logger.Information("Inserting OVM {count}/{total}", count, data.Subjekt.Count);
                        db.OrganVerejneMoci.Add(ovm);
                    }

                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    _logger.Error(e, "Transaction failed.");
                    throw;
                }
            });
            //some addresses in file are corrupted, so we need to fix it.
            //When they upgrade file correctly, then we can remove this fix.
            await FixAddresses();
            _logger.Information("OVM download finished successfully");
        }

        private static async Task FixAddresses()
        {
            _logger.Information("Updating incorrect address references");
            using (var db = new DbEntities())
            {
                //doplnění
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 75701855 where Zkratka = 'Podvihov'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 3134717 where Zkratka = 'Hrabova'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 7530200 where Zkratka = 'PrdbceVIII'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 9401008 where Zkratka = 'Bonkov'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 440434 where Zkratka = 'Bosovice'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 15675912 where Zkratka = 'Svojsin'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 5265967 where Zkratka = 'KrznviceCh'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 16729668 where Zkratka = 'LukavecLi'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 18026834 where Zkratka = 'Lukova'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 18652255 where Zkratka = 'Mastnik'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 6661220 where Zkratka = 'Pohnani'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 3766217 where Zkratka = 'ChlmKrhvce'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 18201831 where Zkratka = 'Charovice'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 14116707 where Zkratka = 'HorniLomna'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 27506908 where Zkratka = 'JrsvNzrkou'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 12105287 where Zkratka = 'JlvDrzkova'");
                await db.Database.ExecuteSqlRawAsync("update dbo.AdresaOvm set KrajNazev = N'Královéhradecký' where Id = 9864792");
                //oprava
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 83068392 where Zkratka = 'Pesvice'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 79245129 where Zkratka = 'Veprova'");
                await db.Database.ExecuteSqlRawAsync("update dbo.OrganVerejneMoci set AdresaOvmId = 5982341 where Zkratka = 'Kutrovice'");
            }
        }

    }
}
