using System.Net.Mime;
using System.Text;
using System.Text.Json;

using Devmasters;

using HlidacStatu.Analysis.Page.Area;

using NeoSmart.PrettySize;

namespace BlurredPageMinion
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly HttpClient apiClient;
        private readonly HttpClient httpClient;
        public Worker(ILogger<Worker> logger, IHttpClientFactory fa)
        {
            this.logger = logger;
            this.apiClient = fa.CreateClient("api");
            this.httpClient = fa.CreateClient("common");

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TimeSpan waitForNextReq = TimeSpan.FromMinutes(1);
                try
                {
                    waitForNextReq = await ProcessTask();

                    long memBefore = GC.GetTotalMemory(false);
                    for (int gen = 0; gen < System.GC.MaxGeneration; gen++)
                    {
                        System.Runtime.GCSettings.LargeObjectHeapCompactionMode = System.Runtime.GCLargeObjectHeapCompactionMode.CompactOnce;
                        System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Batch;

                        System.GC.Collect(gen, GCCollectionMode.Forced);
                        System.GC.WaitForPendingFinalizers();
                    }
                    System.GC.Collect();
                    System.GC.WaitForPendingFinalizers();
                    long memAter = GC.GetTotalMemory(false);

                    logger.LogInformation($"GC forced and done. Memory usage changed from {PrettySize.Format(memBefore)} to {PrettySize.Format(memAter)}");
                }
                catch (Exception e)
                {
                    logger.LogCritical(e, "Unhandled ProcessTask Error");
                    ExceptionHandling.SendLogToServer("Unhandled ProcessTask Error\n" + e.ToString(), httpClient);
                }
                await Task.Delay(waitForNextReq, stoppingToken);
            }
        }
        //static DetectText.ModelFeeder mf = new DetectText.ModelFeeder((int)(2));

        async Task<TimeSpan> ProcessTask()
        {
            Models.BpGet item = null;
            string reqGet = null;
            try
            {
                reqGet = apiClient.GetStringAsync("https://api.hlidacstatu.cz/api/v2/bp/Get")
                   .ConfigureAwait(false).GetAwaiter().GetResult();
                if (reqGet == null)
                    return TimeSpan.FromSeconds(10);


                item = JsonSerializer.Deserialize<Models.BpGet>(reqGet);
                if (item == null)
                {
                    logger.LogError("cannot read JSON from API. JSON:\n" + reqGet);
                    return TimeSpan.FromMinutes(1);
                }
                if (string.IsNullOrEmpty(item.smlouvaId))
                {
                    logger.LogError("SmlouvaId is empty. Probably no more tasks to do. ");
                    return TimeSpan.FromMinutes(10);
                }
            }
            catch (System.Net.Http.HttpRequestException e)
            {
                int statusCode = (int)e.StatusCode;
                if (statusCode >= 500)
                {
                    logger.LogError(e, "cannot read JSON from API. API error " + statusCode);
                    return TimeSpan.FromMinutes(1);
                }
                else if (statusCode >= 400)
                {
                    logger.LogError(e, "No more data from API. Probably no more tasks to do." + statusCode);
                    return TimeSpan.FromMinutes(10);
                }
            }
            catch (Exception e)
            {

                logger.LogError(e, "cannot read JSON from API. JSON:\n" + reqGet);
                return TimeSpan.FromSeconds(10);
            }

            logger.LogDebug("Smlouva {smlouva} found {pocetPriloh} priloh", item.smlouvaId, item.prilohy?.Count());

            List<AnalyzedPdf> hotovePrilohy = new List<AnalyzedPdf>();


            foreach (var p in item.prilohy)
            {
                string tmpFile = System.IO.Path.GetTempFileName();
                string tmpPdf = tmpFile + ".pdf";

                try
                {
                    using (Stream s = await httpClient.GetStreamAsync(p.url).ConfigureAwait(false))
                    {
                        using (var fs = new FileStream(tmpPdf, FileMode.CreateNew))
                        {
                            await s.CopyToAsync(fs);
                        }
                    }
                    //File.WriteAllBytes(tmpPdf, await httpClient.GetByteArrayAsync(p.url));
                }
                catch (System.Net.Http.HttpRequestException e)
                {
                    int statusCode = (int)e.StatusCode;
                    if (statusCode >= 500)
                    {
                        logger.LogError(e, $"cannot download priloha from {p.url.ShortenMeInMiddle(60)}. Http code " + statusCode);
                        continue;
                    }
                    else if (statusCode >= 400)
                    {
                        logger.LogError(e, $"cannot download priloha from {p.url.ShortenMeInMiddle(60)}. Http code " + statusCode);
                        continue;
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"cannot download priloha from {p.url.ShortenMeInMiddle(60)}.");
                    continue;
                }

                Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                try
                {

                    if (HasPDFHeader(tmpPdf))
                    {

                        sw.Start();
                        AnalyzedPdf p_done = null;
                        p_done = AnalyzePdfFromCmd.AnalyzePDF(tmpPdf, Settings.Debug);
                        p_done.uniqueId = p.uniqueId;
                        hotovePrilohy.Add(p_done);
                        sw.Stop();
                        if (p_done != null && p_done?.pages?.Length>0)
                        {
                            double doneInSec = sw.Elapsed.TotalSeconds;
                            var stats = $"File with {p_done.pages.Length} pages parsed in {doneInSec:N2} sec. "
                                + $"It's {p_done.pages.Length / doneInSec:N2} pages/s or {doneInSec / p_done.pages.Length:N2} seconds/page.";
                            logger.LogInformation(stats);                            
                            Console.WriteLine(stats);
                        }
                    }//HasPDFHeader

                }
                catch (Exception e)
                {
                    logger.LogError(e, "ERROR: {smlouva}  cannot get file.", item.smlouvaId);
                    System.IO.File.Copy(tmpPdf, tmpPdf + ".error", true);
                }
                finally
                {
                    if (sw.IsRunning)
                        sw.Stop();
    
                    try
                    {

                        Devmasters.IO.IOTools.DeleteFile(tmpPdf, TimeSpan.FromSeconds(50), TimeSpan.FromSeconds(2), true);

                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "ERROR: {smlouva} priloha {priloha} cannot delete {file}.", item.smlouvaId, p.uniqueId, tmpPdf);
                        //Devmasters.IO.IOTools.DeleteFile(tmpPdf);
                    }
                }
            }
            var final = new Models.BpSave();
            final.smlouvaId = item.smlouvaId;
            final.prilohy = hotovePrilohy.ToArray();

            var content = new StringContent(JsonSerializer.Serialize(final), Encoding.UTF8, MediaTypeNames.Application.Json);
            bool saveRes = false;
            saveRes = SaveResult(content, item);
            if (saveRes)
                return TimeSpan.FromSeconds(2);
            else
            {
                System.Threading.Thread.Sleep(5_000);
                saveRes = SaveResult(content, item);
                if (saveRes)
                    return TimeSpan.FromSeconds(2);
                else
                {
                    System.Threading.Thread.Sleep(5_000);
                    saveRes = SaveResult(content, item);
                }
                if (saveRes)
                    return TimeSpan.FromSeconds(2);
                else
                {
                    System.Threading.Thread.Sleep(5_000);
                    saveRes = SaveResult(content, item);
                    if (saveRes)
                        return TimeSpan.FromSeconds(2);
                    else
                    {
                        logger.LogError("smlouva {smlouva} cannot save result to API.", item.smlouvaId);
                        return TimeSpan.FromMinutes(1);

                    }
                }

            }
        }

        bool SaveResult(StringContent content, Models.BpGet item)
        {
            HttpResponseMessage reqSave = null;

            try
            {
                reqSave = apiClient.PostAsync("https://api.hlidacstatu.cz/api/v2/bp/Save", content)
                    .ConfigureAwait(false).GetAwaiter().GetResult();
                if (reqSave.IsSuccessStatusCode)
                {
                    logger.LogInformation("smlouva {smlouva} priloha Saved result to API.", item.smlouvaId);

                    //System.Threading.Thread.Sleep(TimeSpan.FromMinutes(1));
                    return true;
                }
                else
                {
                    logger.LogError("smlouva {smlouva} cannot save result to API. Status code {statuscode}:\n", item.smlouvaId, reqSave.StatusCode);

                    return false;

                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "smlouva {smlouva} cannot save result to API. JSON:\n" + reqSave, item.smlouvaId);

                return false;
            }
        }

        Models.BpGet GetFakeSmlouva()
        {
            return new Models.BpGet()
            {
                smlouvaId = "235761",
                prilohy = new Models.BpGet.BpGPriloha[] {
               new Models.BpGet.BpGPriloha(){
                    uniqueId="628b005ca831a04ebc7a1eddeadd47c44d69fe35fa79abdd9c3d5b36003d5ae4",
                     url ="https://www.hlidacstatu.cz/KopiePrilohy/235761?hash=628b005ca831a04ebc7a1eddeadd47c44d69fe35fa79abdd9c3d5b36003d5ae4"
                },
               new Models.BpGet.BpGPriloha(){
                    uniqueId="a6978610b50b525ce34e6bc35503875b5a1aa0865f6e137f23b4e93e60d96d0f",
                     url ="https://www.hlidacstatu.cz/KopiePrilohy/235761?hash=a6978610b50b525ce34e6bc35503875b5a1aa0865f6e137f23b4e93e60d96d0f"
                },
              }
            };
        }

        static byte[] pdfheader = new byte[] { 37, 80, 68, 70 };

        public static bool HasPDFHeader(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                return false;
            if (!System.IO.File.Exists(filename))
                return false;

            byte[] b = new byte[4];
            using (var r = System.IO.File.OpenRead(filename))
            {
                r.Read(b, 0, 4);
            }
            bool valid = true;
            for (int i = 0; i < 4; i++)
            {
                valid = valid && b[i] == pdfheader[i];
            }
            return valid;
        }
    }
}