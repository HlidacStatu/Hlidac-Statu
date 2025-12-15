using CsvHelper;
using CsvHelper.Configuration;
using HlidacStatu.RegistrVozidel.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Channels;

namespace HlidacStatu.RegistrVozidel
{

    public class Importer
    {
        private static readonly ILogger _logger = Serilog.Log.ForContext<Importer>();


        // co se má udělat s entitou
        private record PendingChange<T>(T Entity, EntityState State);
        // dávka změn
        private record SaveBatch<T>(int RowFrom, int RowTo, List<PendingChange<T>> Changes);


        public class OpenDataDownload
        {



            public List<OpenDataFile> MesicniDavka { get; set; } = new();
            public DateTime MesicRok { get; set; }
            public class OpenDataFile
            {
                public enum Typy : int
                {
                    vypis_vozidel = 3,
                    technicke_prohlidky = 4,
                    vozidla_vyrazena_z_provozu = 6,
                    vozidla_dovoz = 7,
                    vozidla_doplnkove_vybaveni = 8,
                    zpravy_vyrobce_zastupce = 9,
                    vlastnik_provozovatel_vozidla = 10
                }

                string _directory = "";
                public string Directory
                {
                    get
                    {
                        return _directory;
                    }
                    set
                    {
                        var tmp = value;
                        if (tmp.EndsWith(System.IO.Path.DirectorySeparatorChar) == false)
                            tmp += System.IO.Path.DirectorySeparatorChar;
                        _directory = tmp;
                    }

                }
                public string Nazev { get; set; }
                public string NormalizedNazev { get; set; }
                public string Guid { get; set; }
                public Typy Typ { get; set; }
                public DateTime Vygenerovano { get; set; }
                public int Skip { get; set; }
            }
        }


        static async Task<string?> GetRemoteFileNameAsync(Uri url, CancellationToken ct = default)
        {
            using var http = new HttpClient();
            using var req = new HttpRequestMessage(HttpMethod.Head, url);
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();
            return ExtractFileName(resp.Content.Headers);
        }

        static string? ExtractFileName(HttpContentHeaders headers)
        {
            var cd = headers.ContentDisposition;
            if (cd is null) return null;

            // RFC 5987 (filename*) má přednost, pak filename
            var name = cd.FileNameStar ?? cd.FileName;
            if (string.IsNullOrWhiteSpace(name)) return null;

            return name.Trim().Trim('"'); // někdy je v uvozovkách
        }
        static async Task DownloadToFileWithProgressAsync(
      HttpClient http,
      Uri url,
      string targetPath,
      IProgress<DownloadProgress>? progress = null,
      CancellationToken ct = default)
        {
            var tmpPath = targetPath + ".part";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            var total = resp.Content.Headers.ContentLength; // může být null (chunked)
            progress?.Report(new DownloadProgress(0, total));

            await using var netStream = await resp.Content.ReadAsStreamAsync(ct);

            await using var fileStream = new FileStream(
                tmpPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 1024 * 1024,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan);

            var buffer = new byte[1024 * 1024]; // 1 MB
            long downloaded = 0;

            // throttling výpisu
            var lastReport = Environment.TickCount64;
            const int reportEveryMs = 1000;

            while (true)
            {
                int read = await netStream.ReadAsync(buffer, ct);
                if (read == 0) break;

                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
                downloaded += read;

                var now = Environment.TickCount64;
                if (now - lastReport >= reportEveryMs)
                {
                    lastReport = now;
                    progress?.Report(new DownloadProgress(downloaded, total));
                }
            }

            await fileStream.FlushAsync(ct);
            fileStream.Close();
            progress?.Report(new DownloadProgress(downloaded, total));

            // atomický přesun
            System.IO.File.Move(tmpPath, targetPath, overwrite: true);
        }

        public readonly record struct DownloadProgress(long DownloadedBytes, long? TotalBytes)
        {
            public double? Percent =>
                TotalBytes is long t && t > 0 ? (double)DownloadedBytes * 100d / t : null;
        }
        public async static Task<List<OpenDataDownload>> GetAvailableDownloadsAsync(bool all, string fulldir)
        {

            var progress = new Progress<DownloadProgress>(p =>
            {
                if (p.Percent is double pct)
                    Console.WriteLine($"\rDownloaded {p.DownloadedBytes:n0} / {p.TotalBytes:n0} bytes ({pct:0.00}%)   ");
                else
                    Console.WriteLine($"\rDownloaded {p.DownloadedBytes:n0} bytes   ");
            });

            List<OpenDataDownload> downloads = new List<OpenDataDownload>();
            using var wc = new Devmasters.Net.HttpClient.URLContent("https://download.dataovozidlech.cz");
            using var httpClient = Devmasters.Net.HttpClient.Simple.SharedClient(TimeSpan.FromMinutes(5));


            var links = new Dictionary<string, OpenDataDownload.OpenDataFile.Typy>()
                {
                    { "https://download.dataovozidlech.cz/vypiszregistru/vypisvozidel", OpenDataDownload.OpenDataFile.Typy.vypis_vozidel },
                    { "https://download.dataovozidlech.cz/vypiszregistru/vozidlavyrazenazprovozu", OpenDataDownload.OpenDataFile.Typy.vozidla_vyrazena_z_provozu },
                    { "https://download.dataovozidlech.cz/vypiszregistru/technickeprohlidky", OpenDataDownload.OpenDataFile.Typy.technicke_prohlidky },
                    { "https://download.dataovozidlech.cz/vypiszregistru/vozidladovoz", OpenDataDownload.OpenDataFile.Typy.vozidla_dovoz },
                    { "https://download.dataovozidlech.cz/vypiszregistru/vozidladoplnkovevybaveni", OpenDataDownload.OpenDataFile.Typy.vozidla_doplnkove_vybaveni },
                    { "https://download.dataovozidlech.cz/vypiszregistru/zpravyvyrobcezastupce", OpenDataDownload.OpenDataFile.Typy.zpravy_vyrobce_zastupce },
                    { "https://download.dataovozidlech.cz/vypiszregistru/vlastnikprovozovatelvozidla", OpenDataDownload.OpenDataFile.Typy.vlastnik_provozovatel_vozidla },
                };

            downloads.Add(new OpenDataDownload() { MesicRok = DateTime.Now });
            foreach (var link in links)
            {
                var fileName = await GetRemoteFileNameAsync(new Uri(link.Key));
                if (fileName is null)
                {
                    _logger.Error("Cannot get remote file name for {url}", link.Key);
                    continue;
                }
                    var df = new OpenDataDownload.OpenDataFile
                        {
                            Directory = fulldir,
                            Nazev = fileName,
                            NormalizedNazev = fileName,
                            Guid = Guid.NewGuid().ToString(),
                            Typ = link.Value,
                            Vygenerovano = DateTime.Now,
                            Skip = 0
                        };
                downloads[0].MesicniDavka.Add(df);

                if (System.IO.File.Exists(df.Directory + df.Nazev))
                {
                    _logger.Information("File {file} already exists, skipping download", fileName);
                }
                else
                {
                    await DownloadToFileWithProgressAsync(httpClient, new Uri(link.Key), df.Directory + df.Nazev, progress);
                }
            }


            if (all == false)
            {
                return downloads;
            }


            DateTime minDate = new DateTime(2024, 1, 1);

            if (false) //debug proxy
            {
                System.Net.Http.HttpClient.DefaultProxy = new System.Net.WebProxy("127.0.0.1", 8888);
                ServicePointManager.ServerCertificateValidationCallback =
                    delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
            }
            DateTime lastDate = DateTime.Now.Date.AddMonths(-1).AddDays(-1 * (DateTime.Now.Date.Day - 1));

            //load HP and get cookies
            var _hPhttpContext = wc.GetContent();
            var realLastDate = lastDate;
            do
            {
                string odJsonUrl = $"https://download.dataovozidlech.cz/api/datareport/regularReports?mesic={realLastDate.Month:00}&rok={realLastDate.Year}";
                using (var wcdata = new Devmasters.Net.HttpClient.URLContent(odJsonUrl, _hPhttpContext.Context))
                {
                    wcdata.IgnoreHttpErrors = true;
                    wcdata.Tries = 1;
                    wcdata.RequestParams.Accept = "application/json, text/plain, */*";
                    wcdata.RequestParams.Headers["Accept-Language"] = "en-US,en;q=0.9";
                    wcdata.RequestParams.Headers["_ren"] = _hPhttpContext.Context.Headers["_ren"];
                    var sJson = wcdata.GetContent();

                    if (sJson.Text?.Length > 100)
                    {
                        //realLastDate contains last date we have data for
                        break;
                    }
                }
                realLastDate = realLastDate.AddMonths(-1);
                if (realLastDate < minDate)
                    break;
            } while (true);

            DateTime[] datesToProcess = new DateTime[] { realLastDate };
            if (all)
            {
                var datesList = new System.Collections.Generic.List<DateTime>();
                DateTime dt = minDate;
                while (dt < realLastDate)
                {
                    datesList.Add(dt);
                    dt = dt.AddMonths(1);
                }
                datesList.AddRange(datesToProcess);
                datesToProcess = datesList.OrderBy(m => m).ToArray();
            }

            Console.WriteLine($"Processing dates {string.Join(", ", datesToProcess)}");
            foreach (var d in datesToProcess)
            {
                string odJsonUrl = $"https://download.dataovozidlech.cz/api/datareport/regularReports?mesic={realLastDate.Month:00}&rok={realLastDate.Year}";
                OpenDataDownload.OpenDataFile[] download = null;
                using (var wcdata = new Devmasters.Net.HttpClient.URLContent(odJsonUrl, _hPhttpContext.Context))
                {
                    wcdata.IgnoreHttpErrors = true;
                    wcdata.Tries = 1;
                    wcdata.RequestParams.Accept = "application/json, text/plain, */*";
                    wcdata.RequestParams.Headers["Accept-Language"] = "en-US,en;q=0.9";
                    wcdata.RequestParams.Headers["_ren"] = _hPhttpContext.Context.Headers["_ren"];
                    var sJson = wcdata.GetContent();
                    download = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenDataDownload.OpenDataFile[]>(sJson.Text);
                }
                downloads.Add(new OpenDataDownload()
                {
                    MesicRok = d,
                    MesicniDavka = download.ToList()
                });
            }

            return downloads;
        }

        public class BadRow
        {
            public int RowNumber { get; set; }
            public int FieldCount { get; set; }
            public string ErrorMessage { get; set; }
        }


        async Task SaveWorkerAsync<T>(
          ChannelReader<SaveBatch<T>> reader,
          Func<dbCtx> dbFactory,
          Action<string> log,
          CancellationToken ct)
          where T : class
        {
            await foreach (var batch in reader.ReadAllAsync(ct))
            {
                await using var db = dbFactory();

                // Připojit entity do nového DbContextu a nastavit stav
                foreach (var ch in batch.Changes)
                {
                    db.Entry(ch.Entity).State = ch.State; // Added / Modified
                }

                var sw = new Devmasters.DT.StopWatchEx();
                sw.Start();
                await db.SaveChangesAsync(ct);
                sw.Stop();

                log($"Saved rows {batch.RowFrom}-{batch.RowTo} ( {batch.Changes.Count}/{reader.Count} ) in {sw.ExactElapsedMiliseconds} ms");
            }
        }


        public async Task ImportAsync<T>(
        OpenDataDownload.OpenDataFile file,
        int step = 100,
        Func<dbCtx> dbFactory = null,
        CancellationToken ct = default)
        where T : class
        {
            int rows = 0;
            int badCount = 0;

            dbFactory = dbFactory ?? (() => new dbCtx());

            // bounded fronta, aby se to při extrémní rychlosti parsování nerozjelo do RAM
            var channel = Channel.CreateBounded<SaveBatch<T>>(new BoundedChannelOptions(capacity: 200)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });

            var csvConfiguration = new CsvConfiguration(HlidacStatu.Util.Consts.czCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                MissingFieldFound = null,
                BadDataFound = (BadDataFoundArgs args) =>
                {
                    FileLog(args.Context.Parser);
                    badCount++;
                }
            };

            // 20 paralelních workerů
            var workers = Enumerable.Range(0, 20)
                .Select(i => SaveWorkerAsync<T>(
                    channel.Reader,
                    dbFactory,
                    msg => Console.WriteLine($"[save#{i:00}] {msg}"),
                    ct))
                .ToArray();


            var swCsv = new Devmasters.DT.StopWatchEx();

            var pending = new List<PendingChange<T>>(capacity: step);
            int batchStartRow = 0;

            using (var reader = new StreamReader(file.Directory + file.NormalizedNazev, System.Text.Encoding.UTF8))
            using (var csv = new CsvReader(reader, csvConfiguration))
            {
                await csv.ReadAsync();
                csv.ReadHeader();

                swCsv.Start();

                while (await csv.ReadAsync())
                {
                    ct.ThrowIfCancellationRequested();

                    rows++;
                    if (rows < file.Skip)
                        continue;

                    try
                    {
                        T rec = csv.GetRecord<T>();

                        // dup-check (zůstává v hlavním vlákně)
                        var check = await ((Models.ICheckDuplicate)rec).CheckDuplicateAsync();

                        if (check == Models.ICheckDuplicate.DuplicateCheckResult.NoDuplicate)
                        {
                            pending.Add(new PendingChange<T>(rec, EntityState.Added));
                        }
                        else if (check == Models.ICheckDuplicate.DuplicateCheckResult.SamePrimaryKeyOtherChecksum)
                        {
                            pending.Add(new PendingChange<T>(rec, EntityState.Modified));
                        }
                    }
                    catch
                    {
                        FileLog(csv.Parser);
                        badCount++;
                    }

                    if (rows % step == 0)
                    {
                        swCsv.Stop();
                        Console.Write($"in {swCsv.ExactElapsedMiliseconds} | row {rows}, bads so far: {badCount}. Enqueue batch... ");

                        // pošleme dávku do background ukládání (hlavní while jen krátce čeká, když je fronta plná)
                        var batch = new SaveBatch<T>(
                            RowFrom: batchStartRow == 0 ? (rows - step + 1) : batchStartRow,
                            RowTo: rows,
                            Changes: pending);

                        batchStartRow = rows + 1;

                        pending = new List<PendingChange<T>>(capacity: step);
                        await channel.Writer.WriteAsync(batch, ct);

                        Console.WriteLine("queued.");
                        swCsv.Restart();
                    }
                }
            }

            // dojet poslední nedokončený batch
            if (pending.Count > 0)
            {
                var batch = new SaveBatch<T>(
                    RowFrom: batchStartRow == 0 ? (rows - pending.Count + 1) : batchStartRow,
                    RowTo: rows,
                    Changes: pending);

                await channel.Writer.WriteAsync(batch, ct);
            }

            channel.Writer.Complete();
            await Task.WhenAll(workers);
        }

        public async Task __ReadCSV_and_DBSyncAsync<T>(OpenDataDownload.OpenDataFile file)
            where T : HlidacStatu.RegistrVozidel.Models.ICheckDuplicate, new()
        {
            int badCount = 0;
            List<BadRow> bads = new List<BadRow>();
            var csvConfiguration = new CsvConfiguration(HlidacStatu.Util.Consts.czCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                MissingFieldFound = null,
                BadDataFound = (BadDataFoundArgs args) =>
                {
                    FileLog(args.Context.Parser);
                    badCount++;
                }
            };

            using var db = new HlidacStatu.RegistrVozidel.Models.dbCtx();

            int step = 100;
            int rows = 0;
            Devmasters.DT.StopWatchEx swCsv = new Devmasters.DT.StopWatchEx();

            using (var reader = new StreamReader(file.Directory + file.NormalizedNazev, System.Text.Encoding.UTF8))
            using (var csv = new CsvReader(reader, csvConfiguration))
            {
                await csv.ReadAsync();
                csv.ReadHeader();
                swCsv.Start();
                while (await csv.ReadAsync())
                {
                    rows++;
                    if (rows < file.Skip)
                        continue;
                    //Console.Write(".");

                    try
                    {
                        T rec = csv.GetRecord<T>();
                        // Do something with the record.
                        //Console.WriteLine($"{rec}");
                        Models.ICheckDuplicate.DuplicateCheckResult check = await rec.CheckDuplicateAsync();
                        if (check == Models.ICheckDuplicate.DuplicateCheckResult.NoDuplicate)
                            db.Add(rec);
                        else if (check == Models.ICheckDuplicate.DuplicateCheckResult.SamePrimaryKeyOtherChecksum)
                        {
                            //update
                            db.Update(rec);
                        }


                    }
                    catch (Exception e)
                    {
                        //Console.WriteLine($"{e.Message}");
                        //throw;
                        FileLog(csv.Parser);
                        badCount++;
                    }
                    if (rows % step == 0)
                    {
                        swCsv.Stop();
                        Console.Write($"in {swCsv.ExactElapsedMiliseconds} | row {rows}, bads so far: {badCount}. Saving changes...");
                        try
                        {
                            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                            sw.Start();
                            _ = await db.SaveChangesAsync();
                            sw.Stop();
                            Console.WriteLine($"{sw.ExactElapsedMiliseconds} saved.");
                            swCsv.Restart();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());

                            throw;
                        }
                    }

                }
            }
            //System.IO.File.WriteAllText(@"c:\!\badrows.json", Newtonsoft.Json.JsonConvert.SerializeObject(bads, Newtonsoft.Json.Formatting.Indented), System.Text.Encoding.UTF8);
            Console.WriteLine($"BadCount:{badCount}");
        }


        static int prevRow = -1;
        void FileLog(IParser parser)
        {
            if (prevRow != parser.Row)
            {
                prevRow = parser.Row;
                System.IO.File.AppendAllText(@"c:\!\badrows.log", $"Row:{parser.Row}, Fields:{parser.Count}\n");
            }
        }
    }
}
