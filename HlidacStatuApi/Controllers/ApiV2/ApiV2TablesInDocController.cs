using HlidacStatu.Entities;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;
using System.Data.SqlClient;
using static HlidacStatu.DS.Api.BlurredPage;
using ILogger = Serilog.ILogger;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [ApiExplorerSettings(IgnoreApi = false)]
    [SwaggerTag("TablesInDoc")]
    [ApiController()]
    [Route("api/v2/tbls")]
    public class ApiV2TablesInDocController : ControllerBase
    {
        private ILogger _logger = Serilog.Log.ForContext<ApiV2TablesInDocController>();


        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Get")]
        public async Task<ActionResult<HlidacStatu.DS.Api.TablesInDoc.Task>> GetTask()
        {
            CheckRoleRecord(this.User.Identity.Name);

            using HlidacStatu.Q.Simple.Queue<HlidacStatu.DS.Api.TablesInDoc.Task> q = new HlidacStatu.Q.Simple.Queue<HlidacStatu.DS.Api.TablesInDoc.Task>(
                HlidacStatu.DS.Api.TablesInDoc.TablesInDocProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                );

            ulong? id = null;
            var sq = q.GetAndAck(out id);
            if (sq == null)
                return StatusCode(404);

            return sq;

        }

        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("GetMore")]
        public async Task<ActionResult<HlidacStatu.DS.Api.TablesInDoc.Task[]>> GetMore(int numberOfTasks)
        {
            CheckRoleRecord(this.User.Identity.Name);

            using HlidacStatu.Q.Simple.Queue<HlidacStatu.DS.Api.TablesInDoc.Task> q = new HlidacStatu.Q.Simple.Queue<HlidacStatu.DS.Api.TablesInDoc.Task>(
                HlidacStatu.DS.Api.TablesInDoc.TablesInDocProcessingQueueName,
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")
                );

            List<HlidacStatu.DS.Api.TablesInDoc.Task> tasks = new List<HlidacStatu.DS.Api.TablesInDoc.Task>();
            for (int i = 0; i < numberOfTasks; i++)
            {
                ulong? id = null;
                HlidacStatu.DS.Api.TablesInDoc.Task sq = q.GetAndAck(out id);
                if (sq == null)
                    break;
                tasks.Add(sq); 
            }

            return tasks.ToArray();

        }
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Save")]
        public async Task<ActionResult> Save([FromBody] HlidacStatu.DS.Api.TablesInDoc.ApiResult2 data)
        {
            CheckRoleRecord(this.User.Identity.Name);

            await HlidacStatu.Lib.Data.External.Tables.TablesInDocs.Minion.SaveFinishedTaskAsync(data);

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
                "TablesInDocsMinion",
                HttpContext?.User?.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                log);

            return StatusCode(200);
        }

        private void CheckRoleRecord(string username)
        {
            return;
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
                _logger.Error(e, "cannot add {username} to the role blurredAPIAccess", username);
            }

        }


        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("AddTask")]
        public async Task<ActionResult<bool>> AddTask(string id, bool force = true)
        {
            return await HlidacStatu.Lib.Data.External.Tables.TablesInDocs.Minion.CreateNewTaskAsync(id,force);
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
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