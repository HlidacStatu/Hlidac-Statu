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

    public partial class Importer
    {
        private static readonly ILogger _logger = Serilog.Log.ForContext<Importer>();


        // co se má udělat s entitou
        private record PendingChange<T>(T Entity, EntityState State);
        // dávka změn
        private record SaveBatch<T>(int RowFrom, int RowTo, List<PendingChange<T>> Changes);


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
          where T : ICheckDuplicate, new()
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
                try
                {
                    await db.SaveChangesAsync(ct);
                    log($"Saved rows {batch.RowFrom}-{batch.RowTo} ( {batch.Changes.Count}/{reader.Count} ) in {sw.ExactElapsedMiliseconds} ms");

                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error saving batch {rowFrom}-{rowTo} of type {type}", batch.RowFrom, batch.RowTo, typeof(T).Name);
                    log($"ERROR {batch.RowFrom}-{batch.RowTo} {e.Message}");

                    throw;
                }
                sw.Stop();

            }
        }


        public async Task ImportAsync(
                OpenDataDownload.OpenDataFile file,
                int step = 100,
                Func<dbCtx> dbFactory = null,
                CancellationToken ct = default)
        {
            switch (file.Typ)
            {
                case OpenDataDownload.OpenDataFile.Typy.vypis_vozidel:
                    await ImportAsync<VypisVozidel>(file, step, dbFactory, ct);
                    break;
                case OpenDataDownload.OpenDataFile.Typy.technicke_prohlidky:
                    await ImportAsync<TechnickeProhlidky>(file, step, dbFactory, ct);
                    break;
                case OpenDataDownload.OpenDataFile.Typy.vozidla_vyrazena_z_provozu:
                    await ImportAsync<VozidlaVyrazenaZProvozu>(file, step, dbFactory, ct);
                    break;
                case OpenDataDownload.OpenDataFile.Typy.vozidla_dovoz:
                    await ImportAsync<VozidlaDovoz>(file, step, dbFactory, ct);
                    break;
                case OpenDataDownload.OpenDataFile.Typy.vozidla_doplnkove_vybaveni:
                    await ImportAsync<VozidlaDoplnkoveVybaveni>(file, step, dbFactory, ct);
                    break;
                case OpenDataDownload.OpenDataFile.Typy.zpravy_vyrobce_zastupce:
                    await ImportAsync<ZpravyVyrobceZastupce>(file, step, dbFactory, ct);
                    break;
                case OpenDataDownload.OpenDataFile.Typy.vlastnik_provozovatel_vozidla:
                    await ImportAsync<VlastnikProvozovatelVozidla>(file, step, dbFactory, ct);
                    break;
                default:
                    throw new NotSupportedException($"File type {file.Typ} is not supported for import.");
            }
        }
        private async Task ImportAsync<T>(
                OpenDataDownload.OpenDataFile file,
                int step = 100,
                Func<dbCtx> dbFactory = null,
                CancellationToken ct = default)
        where T : ICheckDuplicate, new()
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

            _logger.Information("Preparing duplication check for {type}", typeof(T).Name);
            await T.PreDuplication();

            _logger.Information("Parsing CSV {file} for {type}", file.NormalizedNazev, typeof(T).Name);


            var progressWriter = new Devmasters.Batch.ActionProgressWriter(10_000);

            await using var fs = new FileStream(
                file.Directory + file.NormalizedNazev,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1 << 20, // 1 MB
                options: FileOptions.SequentialScan);
            using (var reader = new StreamReader(fs, System.Text.Encoding.UTF8, bufferSize: 1 << 16))
            using (var csv = new CsvReader(reader, csvConfiguration))
            {
                await csv.ReadAsync();
                csv.ReadHeader();

                var total = fs.Length;
                var started = DateTime.Now;

                swCsv.Start();

                while (await csv.ReadAsync())
                {
                    ct.ThrowIfCancellationRequested();

                    rows++;


                    if (rows < file.Skip)
                    {
                        if (rows % 10000 == 0)
                        {
                            progressWriter.Writer(total, fs.Position, started);

                        }
                        continue;
                    }

                    if (rows % 10000 == 0)
                    {
                        progressWriter.Writer(total, fs.Position, started);
                    }

                    try
                    {
                        T rec = csv.GetRecord<T>();

                        // dup-check (zůstává v hlavním vlákně)
                        var check = await rec.CheckDuplicateAsync();

                        if (check == Models.ICheckDuplicate.DuplicateCheckResult.NoDuplicate)
                        {
                            pending.Add(new PendingChange<T>(rec, EntityState.Added));
                        }
                        else if (check == Models.ICheckDuplicate.DuplicateCheckResult.SamePrimaryKeyOtherChecksum)
                        {
                            pending.Add(new PendingChange<T>(rec, EntityState.Modified));
                        }
                    }
                    catch (Exception e)
                    {
                        FileLog(csv.Parser);
                        badCount++;
                    }

                    if (rows % step == 0)
                    {
                        swCsv.Stop();
                        Console.Write($"{typeof(T).Name} in {swCsv.ElapsedMilliseconds} | row {rows}, bads {badCount} | ");
                        //_logger.Debug("{type}: Processed int {elapsed} {rows} rows, bads so far: {badCount}", swCsv.ElapsedMilliseconds, typeof(T).Name, rows, badCount);)
                        if (pending.Any())
                        {                       // pošleme dávku do background ukládání (hlavní while jen krátce čeká, když je fronta plná)
                            Console.Write("Enqueue batch... ");
                            var batch = new SaveBatch<T>(
                                RowFrom: batchStartRow == 0 ? (rows - step + 1) : batchStartRow,
                                RowTo: rows,
                                Changes: pending);

                            batchStartRow = rows + 1;

                            await channel.Writer.WriteAsync(batch, ct);

                            pending = new List<PendingChange<T>>(capacity: step);
                            Console.Write(" queued.");
                        }
                        Console.WriteLine();
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
            _logger.Information("Waiting for {file} to finish processing {type}", file.NormalizedNazev, typeof(T).Name);

            channel.Writer.Complete();
            await Task.WhenAll(workers);

            _logger.Information("Post duplication for {type}", typeof(T).Name);
            await T.PostDuplication();
            _logger.Information("Done {type}", typeof(T).Name);

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
                        //FileLog(csv.Parser);
                        _logger.Error(e, "Error processing {type} row {row} in file {file}",typeof(T).Name, csv.Parser.Row, file.NormalizedNazev);
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
