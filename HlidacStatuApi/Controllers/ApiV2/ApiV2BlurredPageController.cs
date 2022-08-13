

using System.Data;
using System.Data.SqlClient;

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
            public BpGet request { get; set; }
            public DateTime? taken { get; set; } = null;
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
        static System.Timers.Timer updateQueueTimer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);
        static DateTime lastAddedItemsToQueue = DateTime.MinValue;
        static ApiV2BlurredPageController()
        {
            updateQueueTimer.AutoReset = false;
            updateQueueTimer.Elapsed += UpdateQueueTimer_Elapsed;
            lastAddedItemsToQueue = DateTime.Now;
            idsToProcess = new System.Collections.Concurrent.ConcurrentDictionary<string, processed>();

            new Thread(() =>
            {
                var allIds = SmlouvaRepo.AllIdsFromDB()
                    .Distinct()
                    .Where(m => !string.IsNullOrEmpty(m));

                var countOnStart = idsToProcess.Count;
                HlidacStatuApi.Code.Log.Logger.Info($"Fill queue thread started for {countOnStart} items");

                Devmasters.Batch.ThreadManager.DoActionForAll(allIds,
                k =>
                {
                    var miss = PageMetadataRepo.MissingInPageMetadata(k).Result;
                    if (miss.Any())
                        _ = idsToProcess.TryAdd(k, new processed()
                        {
                            request = new BpGet()
                            {
                                smlouvaId = k,
                                prilohy = miss
                                    .Select(priloha => new BpGet.BpGPriloha()
                                    {
                                        uniqueId = priloha.UniqueHash(),
                                        url = priloha.GetUrl(k, false)
                                    }
                                    )
                                    .ToArray()
                            }, taken=null, takenByUser=null
                        });

                    return new Devmasters.Batch.ActionOutputData();
                }, !System.Diagnostics.Debugger.IsAttached, 2, null, null);

                HlidacStatuApi.Code.Log.Logger.Info($"Fill queue thread done for {countOnStart} items, deleted {countOnStart - idsToProcess.Count}");
            }).Start();


            //updateQueueTimer.Start();
        }

        private static void CleanQueue(IEnumerable<string> keysToClean, Func<string, bool> checkKeyAction, Action<string> removeAction)
        {
            //clean already processed smlouvy from the queue
            string[] keys = keysToClean.ToArray();
            Devmasters.Batch.ThreadManager.DoActionForAll(keys,
                k =>
                {
                    if (checkKeyAction == null || checkKeyAction(k))
                    {
                        var miss = PageMetadataRepo.MissingInPageMetadata(k).Result;
                        if (miss.Any() == false && removeAction != null)
                            removeAction(k);
                    }

                    return new Devmasters.Batch.ActionOutputData();
                }, !System.Diagnostics.Debugger.IsAttached, 2, null, null);

        }

        private static void UpdateQueueTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {

            var newIds = SmlouvaRepo.AllIdsFromDB(deleted: null, from: lastAddedItemsToQueue).ToList();
            lastAddedItemsToQueue = DateTime.Now;

            CleanQueue(newIds, null, k => { _ = newIds.Remove(k); });
            updateQueueTimer.Start();
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Get")]
        public async Task<ActionResult<BpGet>> Get()
        {
            CheckRoleRecord(this.User.Identity.Name);

            string nextId = null;
            DateTime now = DateTime.Now;
again:
            lock (lockObj)
            {
                nextId = idsToProcess.FirstOrDefault(m =>
                    m.Value.takenByUser == null
                    || (m.Value.taken != null && (now - m.Value.taken.Value) > MAXDURATION_OF_TASK_IN_MIN)
                ).Key;

                if (nextId == null)
                    return StatusCode(404);
                else if (idsToProcess.ContainsKey(nextId))
                {
                    idsToProcess[nextId].taken = DateTime.Now;
                    idsToProcess[nextId].takenByUser = this.User?.Identity?.Name;
                    var res = idsToProcess[nextId].request;

                    _ = justInProcess.TryAdd(nextId, idsToProcess[nextId]);
                    return res;
                }
                else
                    return StatusCode(404);
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Save")]
        public async Task<ActionResult> Save([FromBody] BpSave data)
        {
            CheckRoleRecord(this.User.Identity.Name);

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
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Log")]
        public async Task<ActionResult> Log([FromBody] string log)
        {
            CheckRoleRecord(this.User.Identity.Name);

            HlidacStatuApi.Code.Log.Logger.Error(
                "{action} {from} {user} {ip} {message}",
                "RemoteLog",
                "BlurredPageMinion",
                HttpContext?.User?.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                log);

            return StatusCode(200);
        }

        private static void CheckRoleRecord(string username)
        {
            //check if user is in blurredAPIAccess roles
            try
            {
                var found = HlidacStatu.Connectors.DirectDB.GetList<string, string>(
                    "select u.Id, ur.UserId from AspNetUsers u left join AspNetUserRoles ur on u.id = ur.UserId and ur.RoleId='e9a30ca6-8aa7-423c-88f2-b7dd24eda7f8' where u.UserName = @username",
                    System.Data.CommandType.Text, new IDataParameter[] { new SqlParameter("username", username) }
                    );
                if (found.Count() == 0)
                    return;
                if (found.Count() == 1 && found.First().Item2 == null)
                {
                    HlidacStatu.Connectors.DirectDB.NoResult(
                        @"insert into AspNetUserRoles select  (select id from AspNetUsers where Email like @username) as userId,'e9a30ca6-8aa7-423c-88f2-b7dd24eda7f8' as roleId",
                        System.Data.CommandType.Text, new IDataParameter[] { new SqlParameter("username", username) }
                        );
                }

            }
            catch (Exception e)
            {
                HlidacStatuApi.Code.Log.Logger.Error("cannot add {username} to the role blurredAPIAccess", e, username);
            }

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
                    IEnumerable<PageMetadata>? blurredPages = pagesMD.Where(m => m.PrilohaId == pril.UniqueHash());

                    if (blurredPages.Any())
                    {
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
                        pb.Created = DateTime.Now;
                        pril.BlurredPages = pb;
                    }
                    else
                    {
                        if (pril.BlurredPages != null)
                        {
                            //keep
                        }
                        else
                        {
                            pril.BlurredPages = null;
                        }
                    }
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
            var inProcess = idsToProcess.Where(m => m.Value.taken != null);
            var res = new BlurredPageStatistics()
            {
                total = idsToProcess.Count,
                currTaken = inProcess.Count(),
                totalFailed = inProcess.Count(m => (now - m.Value.taken.Value) > MAXDURATION_OF_TASK_IN_MIN)
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

            res.longestTasks = justInProcess.OrderByDescending(o => (now - o.Value.taken.Value).TotalSeconds)
                            .Select(m => new BlurredPageStatistics.perItemStat<decimal>() { email = m.Value.takenByUser, count = (decimal)(now - m.Value.taken.Value).TotalSeconds })
                            .ToArray();
            res.avgTaskLegth = justInProcess
                    .GroupBy(k => k.Value.takenByUser, v => v, (k, v) => new BlurredPageStatistics.perItemStat<decimal>()
                    {
                        email = k,
                        count = (decimal)v.Average(a => (now - a.Value.taken.Value).TotalSeconds)
                    })
                    .OrderByDescending(o => o.count)
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