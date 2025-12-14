using CsvHelper;
using CsvHelper.Configuration;
using Elastic.CommonSchema;
using HlidacStatu.RegistrVozidel.Models;
using Microsoft.EntityFrameworkCore;
using Serilog.Sinks.Http.Private;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace HlidacStatu.RegistrVozidel
{

    public class Importer
    {

        // co se má udělat s entitou
        private record PendingChange<T>(T Entity, EntityState State);
        // dávka změn
        private record SaveBatch<T>(int RowFrom, int RowTo, List<PendingChange<T>> Changes);


        public class OpenDataDownload
        {



            public OpenDataFile[] MesicniDavka { get; set; }
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

        public static List<OpenDataDownload> GetAvailableDownloads(bool all)
        {
            DateTime minDate = new DateTime(2024, 1, 1);

            if (false) //debug proxy
            {
                System.Net.Http.HttpClient.DefaultProxy = new System.Net.WebProxy("127.0.0.1", 8888);
                ServicePointManager.ServerCertificateValidationCallback =
                    delegate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) { return true; };
            }
            DateTime lastDate = DateTime.Now.Date.AddMonths(-1).AddDays(-1 * (DateTime.Now.Date.Day - 1));

            //load HP and get cookies
            using var wc = new Devmasters.Net.HttpClient.URLContent("https://download.dataovozidlech.cz");
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
            List<OpenDataDownload> downloads = new List<OpenDataDownload>();
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
                    MesicniDavka = download
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
