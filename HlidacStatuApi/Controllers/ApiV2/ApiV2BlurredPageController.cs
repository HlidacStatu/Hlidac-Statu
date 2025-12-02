using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;
using Microsoft.Data.SqlClient;
using static HlidacStatu.DS.Api.BlurredPage;
using ILogger = Serilog.ILogger;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [ApiExplorerSettings(IgnoreApi = true)]
    [SwaggerTag("BlurredPage")]
    [ApiController()]
    [Route("api/v2/bp")]
    public class ApiV2BlurredPageController : ControllerBase
    {
        private readonly ILogger _logger = Serilog.Log.ForContext<ApiV2BlurredPageController>();
        
        static long runningSaveThreads = 0;
        static long savingPagesInThreads = 0;
        static long savedInThread = 0;
        static long saved = 0;

        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Get")]
        public async Task<ActionResult<BpTask>> Get()
        {
            CheckRoleRecord(this.User.Identity.Name);

            using HlidacStatu.Q.Simple.Queue<BpTask> q = new HlidacStatu.Q.Simple.Queue<BpTask>(
                HlidacStatu.DS.Api.BlurredPage.BlurredPageProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                );

            ulong? id = null;
            next:
            var sq = q.GetAndAck(out id);
            var platnyZaznam = await SmlouvaRepo.GetPartValueAsync<bool>(sq.smlouvaId, "platnyZaznam");
            if (platnyZaznam == false)
                goto next;

            if (sq == null)
                return StatusCode(404);

            return sq;

        }

        //todo: refactor to channels
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
                    new Thread(async () =>
                        {
                            _ = Interlocked.Increment(ref savedInThread);
                            _ = Interlocked.Increment(ref runningSaveThreads);
                            _ = Interlocked.Add(ref savingPagesInThreads, numOfPages);

                            _logger.Information(
                                "{action} {code} for {part} for {pages}.",
                                "starting",
                                "thread",
                                "ApiV2BlurredPageController.BpSave",
                                numOfPages);
                            Devmasters.DT.StopWatchEx sw = new Devmasters.DT.StopWatchEx();
                            sw.Start();

                            var success = await SaveDataAsync(data);
                            if (!success)
                                _ = await SaveDataAsync(data);

                            sw.Stop();
                            _logger.Information(
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
                    var success = await SaveDataAsync(data);
                    if (!success)
                        _ = await SaveDataAsync(data);
                }
            }

            _ = Interlocked.Increment(ref savedInThread);

            //_ = justInProcess.Remove(data.smlouvaId, out _);

            return StatusCode(200);
        }


        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Log")]
        public async Task<ActionResult> Log([FromBody] string log)
        {
            CheckRoleRecord(this.User.Identity.Name);

            _logger.Error(
                "{action} {from} {user} {ip} {message}",
                "RemoteLog",
                "BlurredPageMinion",
                HttpContext?.User?.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                log);

            return StatusCode(200);
        }

        private void CheckRoleRecord(string username)
        {
            //check if user is in blurredAPIAccess roles
            try
            {
                var found = HlidacStatu.Connectors.DirectDB.Instance.GetList<string, string>(
                    "select u.Id, ur.UserId from AspNetUsers u left join AspNetUserRoles ur on u.id = ur.UserId and ur.RoleId='e9a30ca6-8aa7-423c-88f2-b7dd24eda7f8' where u.UserName = @username",
                    System.Data.CommandType.Text, new IDataParameter[] { new SqlParameter("username", username) }
                    );
                if (found.Count() == 0)
                    return;
                if (found.Count() == 1 && found.First().Item2 == null)
                {
                    HlidacStatu.Connectors.DirectDB.Instance.NoResult(
                        @"insert into AspNetUserRoles select  (select id from AspNetUsers where Email like @username) as userId,'e9a30ca6-8aa7-423c-88f2-b7dd24eda7f8' as roleId",
                        System.Data.CommandType.Text, new IDataParameter[] { new SqlParameter("username", username) }
                        );
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "cannot add {username} to the role blurredAPIAccess", username);
            }

        }


        private async Task<bool> SaveDataAsync(BpSave data)
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
                    Task t = PageMetadataRepo.SaveAsync(pm);
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
                        var pb = new Smlouva.Priloha.BlurredPagesStats(blurredPages);
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
                _ = await SmlouvaRepo.SaveAsync(sml, updateLastUpdateValue: false, skipPrepareBeforeSave: true, fireOCRDone:false);

            }
            try
            {
                Task.WaitAll(tasks.ToArray());
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e,
                    "{action} {code} for {part} exception.",
                    "saving",
                    "thread",
                    "ApiV2BlurredPageController.BpSave"
                    );
                return false;
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "PrivateApi")]
        [HttpGet("AddTask")]
        public async Task<ActionResult<bool>> AddTask(string id, bool force = true)
        {
            var sml = await SmlouvaRepo.LoadAsync(id, includePlaintext: false);
            if (sml != null)
            {
                if (sml.Prilohy != null)
                {
                    var toProcess = sml.Prilohy.Where(p => (p.BlurredPages == null || force));
                    if (toProcess.Any())
                    {

                        var request = new BpTask()
                        {
                            smlouvaId = id,
                            prilohy = toProcess
                                .Select(priloha => new BpTask.BpGPriloha()
                                {
                                    uniqueId = priloha.UniqueHash(),
                                    url = priloha.LocalCopyUrl(id, true)
                                }
                                )
                                .ToArray()
                        };

                        using HlidacStatu.Q.Simple.Queue<BpTask> q = new HlidacStatu.Q.Simple.Queue<BpTask>(
                            BlurredPageProcessingQueueName,
                            Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                            );

                        q.Send(request);

                        return true;
                    }
                }
            }
            return false;

        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Stats")]
        public async Task<ActionResult<BlurredPageAPIStatistics>> Stats()
        {
            using HlidacStatu.Q.Simple.Queue<BpTask> q = new HlidacStatu.Q.Simple.Queue<BpTask>(
                BlurredPageProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
            );


            DateTime now = DateTime.Now;
            var res = new BlurredPageAPIStatistics()
            {
                total = q.MessageCount()
            };

            return res;
        }
    }
}