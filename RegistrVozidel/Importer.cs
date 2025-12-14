using CsvHelper;
using CsvHelper.Configuration;
using Elastic.CommonSchema;
using Serilog.Sinks.Http.Private;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HlidacStatu.RegistrVozidel
{
    public class Importer
    {
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

                public string Directory { get; set; }
                public string Nazev { get; set; }
                public string NormalizedNazev { get; set; }
                public string Guid { get; set; }
                public Typy Typ { get; set; }
                public DateTime Vygenerovano { get; set; }
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
        static int badCount = 0;
        public async static Task ReadCSVAsync<T>(OpenDataDownload.OpenDataFile file)
            where T : HlidacStatu.RegistrVozidel.Models.ICheckDuplicate, new()
        {
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

            int rows = 0;
            using (var reader = new StreamReader(file.Directory + file.NormalizedNazev, System.Text.Encoding.UTF8))
            using (var csv = new CsvReader(reader, csvConfiguration))
            {

                await csv.ReadAsync();
                csv.ReadHeader();
                while (await csv.ReadAsync())
                {
                    rows++;
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
                    if (rows % 20 == 0)
                    {
                        Console.WriteLine($"row {rows}, bads so far: {badCount}. Saving changes...");
                        try
                        {
                            _ = await db.SaveChangesAsync();

                        }
                        catch (Exception e)
                        {

                            throw;
                        }
                    }

                }
            }
            //System.IO.File.WriteAllText(@"c:\!\badrows.json", Newtonsoft.Json.JsonConvert.SerializeObject(bads, Newtonsoft.Json.Formatting.Indented), System.Text.Encoding.UTF8);
            Console.WriteLine($"BadCount:{badCount}");
        }
        static int prevRow = -1;
        static void FileLog(IParser parser)
        {
            if (prevRow != parser.Row)
            {
                prevRow = parser.Row;
                System.IO.File.AppendAllText(@"c:\!\badrows.log", $"Row:{parser.Row}, Fields:{parser.Count}\n");
            }
        }
    }
}
