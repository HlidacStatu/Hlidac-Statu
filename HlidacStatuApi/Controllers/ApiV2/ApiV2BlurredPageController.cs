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
        
        private readonly BlurredPageBackgroundQueue _backgroundQueue;

        public ApiV2BlurredPageController(BlurredPageBackgroundQueue backgroundQueue)
        {
            _backgroundQueue = backgroundQueue;
        }

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
                await _backgroundQueue.QueueWorkAsync(data);
                _logger.Information(
                    "Queued {pages} pages for background processing for {smlouvaId}",
                    numOfPages,
                    data.smlouvaId
                );
            }

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