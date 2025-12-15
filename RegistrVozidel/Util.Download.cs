using Serilog;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using static HlidacStatu.RegistrVozidel.Importer;

namespace HlidacStatu.RegistrVozidel
{
    public static partial class Util
    {
        public static class Download
        {
            private static readonly ILogger _logger = Serilog.Log.ForContext<Importer>();


            public static async Task<string?> GetRemoteFileNameAsync(Uri url, CancellationToken ct = default)
            {
                using var http = new HttpClient();
                using var req = new HttpRequestMessage(HttpMethod.Head, url);
                using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                resp.EnsureSuccessStatusCode();
                return ExtractFileName(resp.Content.Headers);
            }

            public static string? ExtractFileName(HttpContentHeaders headers)
            {
                var cd = headers.ContentDisposition;
                if (cd is null) return null;

                // RFC 5987 (filename*) má přednost, pak filename
                var name = cd.FileNameStar ?? cd.FileName;
                if (string.IsNullOrWhiteSpace(name)) return null;

                return name.Trim().Trim('"'); // někdy je v uvozovkách
            }
            public static async Task DownloadToFileWithProgressAsync(
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
                if (all)
                    throw new NotImplementedException("Getting all historical data is not implemented.");

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
                        _logger.Information("Downloading {url}", link.Key);
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



        }
    }

}
