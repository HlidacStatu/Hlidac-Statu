

using Devmasters.Collections;

using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{


    [ApiExplorerSettings(IgnoreApi = true)]
    [SwaggerTag("BlurredPage")]
    [ApiController()]
    [Route("api/v2/bp")]
    public class ApiV2BlurredPageController : ControllerBase
    {
        private class processed
        {
            public DateTime taken { get; set; } = DateTime.MinValue;
            public string takenByUser { get; set; } = null;
        }
        static System.Collections.Concurrent.ConcurrentDictionary<string, processed> idsToProcess = null;
        static long runningSaveThreads = 0;
        static long savingPagesInThreads = 0;
        static long savedInThread = 0;
        static long saved = 0;
        static System.Collections.Concurrent.ConcurrentDictionary<string, processed> justInProcess = new();

        static readonly TimeSpan MAXDURATION_OF_TASK_IN_MIN = TimeSpan.FromHours(6);

        static object lockObj = new object();
        static ApiV2BlurredPageController()
        {
            idsToProcess = new System.Collections.Concurrent.ConcurrentDictionary<string, processed>(
                SmlouvaRepo.AllIdsFromDB()
                    .Distinct()
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ShuffleMe()
                    .Select(m => new KeyValuePair<string, processed>(m, null))
                );
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Get")]
        public async Task<ActionResult<BpGet>> Get()
        {
            string nextId = null;
            DateTime now = DateTime.Now;
again:
            lock (lockObj)
            {
                nextId = idsToProcess.FirstOrDefault(m =>
                    m.Value == null
                    || (m.Value != null && (now - m.Value.taken) > MAXDURATION_OF_TASK_IN_MIN)
                ).Key;
                if (nextId == null)
                    return StatusCode(404);
                else if (idsToProcess.ContainsKey(nextId))
                    idsToProcess[nextId] = new processed() { taken = DateTime.Now, takenByUser = HttpContext.User?.Identity?.Name };
                else
                    goto again;
            }

            if (await PageMetadataRepo.ExistsInPageMetadata(nextId))
            {
                idsToProcess.Remove(nextId, out var dt);
                goto again;
            }

            var sml = await SmlouvaRepo.LoadAsync(nextId, includePrilohy: false);
            if (sml == null || sml?.Prilohy == null)
            {
                idsToProcess.Remove(nextId, out var dt);
                goto again;
            }
            var res = new BpGet();
            res.smlouvaId = nextId;
            res.prilohy = sml.Prilohy
                        .Where(p=>p != null)
                        .Select(m => new BpGet.BpGPriloha()
                        {
                            uniqueId = m.UniqueHash(),
                            url = m.GetUrl(nextId, false)
                        }
                        )
                        .ToArray();
            _ = justInProcess.TryAdd(nextId, idsToProcess[nextId]);
            return res;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Save")]
        public async Task<ActionResult> Save([FromBody] BpSave data)
        {
            if (data.prilohy != null)
            {
                int numOfPages = data.prilohy.Sum(m => m.pages.Count());
                if (numOfPages > 200)
                {
                    new Thread(
                        () =>
                        {
                            _ = Interlocked.Increment(ref savedInThread);
                            _ = Interlocked.Increment(ref runningSaveThreads);
                            _ = Interlocked.Add(ref savingPagesInThreads, numOfPages);

                            HlidacStatuApi.Code.Log.Logger.Info(
                                "{action} {code} for {part} for {pages}.",
                                "starting",
                                "thread",
                                "ApiV2BlurredPageController.BpSave",
                                numOfPages);
                            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                            sw.Start();

                            var success = SaveData(data).ConfigureAwait(false).GetAwaiter().GetResult();
                            if (!success)
                                _ = SaveData(data).ConfigureAwait(false).GetAwaiter().GetResult();

                            sw.Stop();
                            HlidacStatuApi.Code.Log.Logger.Info(
                                "{action} {code} for {part} for {pages} in {duration} sec.",
                                "ends",
                                "thread",
                                "ApiV2BlurredPageController.BpSave",
                                numOfPages,
                                sw.Elapsed.TotalSeconds);

                            _ = Interlocked.Decrement(ref runningSaveThreads);
                            _ = Interlocked.Add(ref savingPagesInThreads, -1 * numOfPages);

                        }
                    ).Start();

                }
                else
                {
                    var success = await SaveData(data);
                    if (!success)
                        _ = await SaveData(data);
                }
            }

            _ = Interlocked.Increment(ref savedInThread);
            
            _ = justInProcess.Remove(data.smlouvaId, out _);

            return StatusCode(200);
        }


        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Log")]
        public async Task<ActionResult> Log([FromBody] string log)
        {
            HlidacStatuApi.Code.Log.Logger.Error(
                "{action} {from} {user} {ip} {message}",
                "RemoteLog",
                "BlurredPageMinion",
                HttpContext?.User?.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                log);

            return StatusCode(200);
        }




        private static async Task<bool> SaveData(BpSave data)
        {
            List<Task> tasks = new List<Task>();
            List<PageMetadata> pagesMD = new List<PageMetadata>();
            foreach (var p in data.prilohy)
            {
                foreach (var page in p.pages)
                {
                    PageMetadata pm = new PageMetadata();
                    pm.SmlouvaId = data.smlouvaId;
                    pm.PrilohaId = p.uniqueId;
                    pm.PageNum = page.page;
                    pm.Blurred = new PageMetadata.BlurredMetadata()
                    {
                        BlackenAreaBoundaries = page.blackenAreaBoundaries
                            .Select(b => new PageMetadata.BlurredMetadata.Boundary()
                            {
                                X = b.x,
                                Y = b.y,
                                Width = b.width,
                                Height = b.height
                            }
                            ).ToArray(),
                        TextAreaBoundaries = page.textAreaBoundaries
                            .Select(b => new PageMetadata.BlurredMetadata.Boundary()
                            {
                                X = b.x,
                                Y = b.y,
                                Width = b.width,
                                Height = b.height
                            }
                            ).ToArray(),
                        AnalyzerVersion = page.analyzerVersion,
                        Created = DateTime.Now,
                        ImageWidth = page.imageWidth,
                        ImageHeight = page.imageHeight,
                        BlackenArea = page.blackenArea,
                        TextArea = page.textArea
                    };
                    pagesMD.Add(pm);
                    var t = PageMetadataRepo.SaveAsync(pm);
                    tasks.Add(t);
                    //t.Wait();
                }
            }


            if (pagesMD.Count > 0)
            {
                var sml = await SmlouvaRepo.LoadAsync(data.smlouvaId);
                foreach (var pril in sml.Prilohy)
                {
                    var blurredPages = pagesMD.Where(m => m.PrilohaId == pril.UniqueHash());

                    var pb = new Smlouva.Priloha.BlurredPagesStats();
                    decimal wholeArea = (decimal)(blurredPages.Sum(m => m.Blurred.BlackenArea) + blurredPages.Sum(m => m.Blurred.TextArea));
                    if (wholeArea == 0)
                        pb.BlurredAreaPerc = 0;
                    else
                        pb.BlurredAreaPerc = (decimal)blurredPages.Sum(m => m.Blurred.BlackenArea)
                            / (decimal)(blurredPages.Sum(m => m.Blurred.BlackenArea) + blurredPages.Sum(m => m.Blurred.TextArea));
                    pb.NumOfBlurredPages = blurredPages.Count(m => m.Blurred.BlackenAreaRatio() >= 0.05m);
                    pb.NumOfExtensivelyBlurredPages = blurredPages.Count(m => m.Blurred.BlackenAreaRatio() >= 0.2m);

                    pb.ListOfExtensivelyBlurredPages = blurredPages
                            .Where(m => m.Blurred.BlackenAreaRatio() >= 0.2m)
                            .Select(m => m.PageNum)
                            .ToArray();

                    pril.BlurredPages = pb;

                }
                _ = await SmlouvaRepo.SaveAsync(sml, updateLastUpdateValue: false, skipPrepareBeforeSave: true);

            }
            try
            {
                Task.WaitAll(tasks.ToArray());
                _ = idsToProcess.Remove(data.smlouvaId, out var dt);
                return true;
            }
            catch (Exception e)
            {
                HlidacStatuApi.Code.Log.Logger.Error(
                    "{action} {code} for {part} exception.",
                    e,
                    "saving",
                    "thread",
                    "ApiV2BlurredPageController.BpSave"
                    );
                return false;
            }
        }


        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Stats")]
        public async Task<ActionResult<BlurredPageStatistics>> Stats()
        {
            DateTime now = DateTime.Now;
            var inProcess = idsToProcess.Where(m => m.Value != null);
            var res = new BlurredPageStatistics()
            {
                total = idsToProcess.Count,
                currTaken = inProcess.Count(),
                totalFailed = inProcess.Count(m => (now - m.Value.taken) > MAXDURATION_OF_TASK_IN_MIN)
            };

            return res;
        }
        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize()]
        [HttpGet("Stats2")]
        public async Task<ActionResult<BlurredPageStatistics>> Stats2()
        {
            if (!
                (this.User?.IsInRole("Admin") == true || this.User?.Identity?.Name == "api@hlidacstatu.cz")
                )
                return StatusCode(403);

            DateTime now = DateTime.Now;
            var res = new BlurredPageStatistics()
            {
                total = idsToProcess.Count,
                currTaken = justInProcess.Count(),
                totalFailed = justInProcess.Count(m => (now - m.Value.taken) > MAXDURATION_OF_TASK_IN_MIN)
            };

            res.runningSaveThreads = Interlocked.Read(ref runningSaveThreads);
            res.savingPagesInThreads = Interlocked.Read(ref savingPagesInThreads);
            res.activeTasks = justInProcess
                    .GroupBy(k => k.Value.takenByUser, v => v, (k, v) => new BlurredPageStatistics.perItemStat<long>() { email = k, count = v.Count() })
                    .ToArray();

            res.longestTasks = justInProcess.OrderByDescending(o => (now - o.Value.taken).TotalSeconds)
                            .Take(20)
                            .Select(m => new BlurredPageStatistics.perItemStat<decimal>() { email = m.Value.takenByUser, count = (decimal)(now - m.Value.taken).TotalSeconds })
                            .ToArray();
            res.avgTaskLegth = justInProcess
                    .GroupBy(k => k.Value.takenByUser, v => v, (k, v) => new BlurredPageStatistics.perItemStat<decimal>()
                    {
                        email = k,
                        count = (decimal)v.Average(a => (now - a.Value.taken).TotalSeconds)
                    })
                    .OrderByDescending(o=>o.count)
                    .ToArray();
            savedInThread = Interlocked.Read(ref savedInThread);

            return res;
        }




        public class BpGet
        {
            public class BpGPriloha
            {
                public string uniqueId { get; set; }
                public string url { get; set; }
            }
            public string smlouvaId { get; set; }
            public BpGPriloha[] prilohy { get; set; }

        }

        public class BpSave
        {
            public string smlouvaId { get; set; }

            public HlidacStatu.Analysis.Page.Area.AnalyzedPdf[] prilohy { get; set; }

        }

    }
}