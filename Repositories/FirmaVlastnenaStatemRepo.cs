using ExcelDataReader;
using HlidacStatu.Caching;
using HlidacStatu.DS.Graphs;
using HlidacStatu.Entities;
using InfluxDB.Client.Api.Domain;
using Microsoft.EntityFrameworkCore;
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

namespace HlidacStatu.Repositories
{
    public static class FirmaVlastnenaStatemRepo
    {
        private static ILogger _logger = Serilog.Log.Logger.ForContext(typeof(FirmaVlastnenaStatemRepo));

        //vcetne strategickych podniku z https://www.mfcr.cz/cs/aktualne/tiskove-zpravy/2020/vlada-schvalila-strategii-vlastnicke-pol-37573
        public static string[] StatniFirmyICO = new string[]
        {
            "00000205", "00000337", "00000345", "00000370", "00000493", "00000515", "00001279", "00001481", "00001490",
            "00002321", "00002674", "00002691",
            "00002739", "00003026", "00007536", "00008141", "00008184", "00009181", "00009334", "00009393", "00009563",
            "00010669", "00011011", "00012033",
            "00012343", "00013251", "00013455", "00014079", "00014125", "00014818", "00015156", "00015270", "00015296",
            "00015415", "00015679", "00024007",
            "00076791", "00086932", "00128201", "00157287", "00157325", "00251976", "00311391", "00514152", "00565253",
            "00577880", "00659819", "00664073",
            "03630919", "13695673", "14450216", "14450241", "14867770", "14888025", "15503852", "17047234", "24272523",
            "24729035", "24821993", "24829871",
            "25059386", "25085531", "25125877", "25255843", "25291581", "25401726", "25634160", "25674285", "25702556",
            "25938924", "26051818", "26162539",
            "26175291", "26206803", "26376547", "26463318", "26470411", "26840065", "26871823", "27145573", "27146235",
            "27195872", "27232433", "27257258",
            "27257517", "27309941", "27364976", "27378225", "27772683", "27786331", "27804721", "27892646", "28196678",
            "28244532", "28255933", "28267141",
            "28707052", "28786009", "28861736", "29372259", "42196451", "43833560", "44269595", "44848943", "45144419",
            "45147965", "45193070", "45273375",
            "45273448", "45274649", "45274827", "45279314", "45534268", "45795908", "46355901", "46504818", "46708707",
            "47114983", "47115726", "47673354",
            "47677543", "48204285", "48291749", "48535591", "49241494", "49241672", "49453866", "49454561", "49710371",
            "49901982", "49973720", "60193468",
            "60193531", "60196696", "60197901", "60698101", "61459445", "61860336", "62413376", "63078333", "63080249",
            "70889953", "70889988", "70890005",
            "70890013", "70890021", "70994226", "70994234"

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



        public static List<string> IcaStatnichFirem(int statniPodil)
        {
            using (DbEntities db = new DbEntities())
            {
                return db.FirmyVlastneneStatem
                    .AsNoTracking()
                    .Where(m => m.Podil >= statniPodil)
                    .Select(m => m.Ico)
                    .ToList();
            }
        }

        public static List<string> IcaStatnichFirem()
        {
            using (DbEntities db = new DbEntities())
            {
                return db.FirmyVlastneneStatem
                    .AsNoTracking()
                    .Select(m => m.Ico)
                    .ToList()
                    //.Union(
                    ;
            }
        }
        

        public static void Repopulate(IEnumerable<FirmaVlastnenaStatem> percList)
        {
            using (DbEntities db = new DbEntities())
            {
                db.Database.ExecuteSqlRaw("TRUNCATE TABLE [FirmyVlastneneStatem]");
                db.FirmyVlastneneStatem.AddRange(percList);
                db.SaveChanges();
            }
            
        }



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


        [Obsolete("Use SeznamOvmIndex")]
        public static string DatafileSeznamDsOvmCached()
            => PermanentCache.GetOrSet("datafile-seznam_ds_ovm.xml",
                _ =>
                {
                    try
                    {
                        return Devmasters.Net.HttpClient.Simple.Get("https://www.mojedatovaschranka.cz/sds/datafile?format=xml&service=seznam_ds_ovm");

                    }
                    catch (Exception e)
                    {
                        _logger.Fatal(e, "cannot download datafile-seznam_ds_ovm.xml from https://www.mojedatovaschranka.cz");
                        try
                        {
                            return Devmasters.Net.HttpClient.Simple.Get("https://somedata.hlidacstatu.cz/appdata/datafile-seznam_ds_ovm.xml");


                        }
                        catch (Exception e2)
                        {
                            _logger.Fatal(e2, "cannot download datafile-seznam_ds_ovm.xml from both sources");
                            return "";
                        }
                    }
                },
                options => options.ModifyEntryOptionsDuration(TimeSpan.FromHours(12), TimeSpan.FromDays(10 * 365))
            );

        //merge of Firmy.GlobalStatistics.VsechnyUradyAsync
        //Tasks.Merk.UpdateListStatniFirmy_InDB
        public async static Task<IEnumerable<FirmaVlastnenaStatem>> Najdi_Firmy_Vlastnene_Statem_z_Primarnich_ZdrojuAsync(
            Action<string> outputWriter = null,
            Action<Devmasters.Batch.ActionProgressData> progressWriter = null
            )
        {
            using DbEntities db = new DbEntities();
            List<string> bagOfIco = new List<string>();

            bagOfIco.AddRange(StatniFirmyICO);

            //vsechny firmy z OR podle pravni formy
            bagOfIco.AddRange(
                db.Firma.Where(m => StatniFirmy_BasedKodPF.Contains(m.Kod_PF ?? -1))
                .AsNoTracking()
                .Select(m => m.ICO)
                .ToList()
            );

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

            //updatni OVM v DB
            await Update_OrganVerejneMoci_in_DBAsync();

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
            var ovmIcoToIgnore_nejsouStatni = db.OrganVerejneMoci
                .AsNoTracking()
                .Join(
                    db.OrganVerejneMoci_KategorieOvm.AsNoTracking(),
                    o => o.IdDS,
                    k => k.IdDS,
                    (o, k) => new { o, k }
                )
                .Where(m => m.o.ICO != null)
                .Where(k => forbiddenOVMKategories.Contains(k.k.KategorieOvm))
                .Select(m => m.o.ICO)
                .Distinct()
                .ToHashSet();

            var allOvm = db.OrganVerejneMoci.AsNoTracking().Select(m => m.ICO).ToList()
                .Except(ovmIcoToIgnore_nejsouStatni) ;
            bagOfIco.AddRange(allOvm);



            //delete duplicates and empties
            bagOfIco = bagOfIco
                .Where(m => string.IsNullOrEmpty(m) == false)
                .Distinct()
                .ToList();
            //rucni smazani
            bagOfIco.Remove("25755277"); //Clovek v tisni
            bagOfIco.Remove("00001350"); //csob


            //najdi vsechny firmy a jejich vazby
            object lockObj = new object();
            Devmasters.Batch.Manager.DoActionForAll<string, object>(bagOfIco,
             (ico, obj) =>
             {
                 var f = Firmy.Get(ico);
                 if (f.Valid && FirmaVlastnenaStatemRepo.MuzeBytStatni_PodlePravniFormaKod(f.Kod_PF))
                 {

                     List<string> subjIco = new List<string>();
                     subjIco.Add(Util.ParseTools.NormalizeIco(f.ICO));
                     var vazby = f.AktualniVazby(Relation.AktualnostType.Aktualni, true);
                     subjIco.AddRange(vazby.Select(m => Util.ParseTools.NormalizeIco(m.To.Id)));
                     foreach (var inIc in subjIco)
                     {
                         var ff = Firmy.Get(inIc);
                         if (ff.Valid && FirmaVlastnenaStatemRepo.MuzeBytStatni_PodlePravniFormaKod(ff.Kod_PF))
                         {
                             lock (lockObj)
                             {
                                 bagOfIco.Add(inIc);
                             }
                         }
                     }
                 }

                 return new Devmasters.Batch.ActionOutputData();
             }, null, outputWriter, progressWriter,
             !System.Diagnostics.Debugger.IsAttached, prefix: "freshList statnifirmy ", monitor: new MonitoredTaskRepo.ForBatch()
             );

            //rucni smazani, kdyz by bylo v podrizenych
            bagOfIco.Remove("25755277"); //rucni smazani Clovek v tisni
            bagOfIco.Remove("00001350"); //csob



            return null;
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
                    await db.Database.ExecuteSqlRawAsync("truncate table AdresaOvm");
                    await db.Database.ExecuteSqlRawAsync("truncate table PravniFormaOvm");
                    await db.Database.ExecuteSqlRawAsync("truncate table TypOvm");
                    await db.Database.ExecuteSqlRawAsync("truncate table OrganVerejneMoci");
                    await db.Database.ExecuteSqlRawAsync("truncate table OrganVerejneMoci_KategorieOvm");

                    foreach (var ovm in data.Subjekt)
                    {
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
