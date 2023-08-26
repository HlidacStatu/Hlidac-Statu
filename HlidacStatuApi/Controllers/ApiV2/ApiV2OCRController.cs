using HlidacStatu.Entities;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;
using System.Data.SqlClient;
using static HlidacStatu.DS.Api.BlurredPage;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [ApiExplorerSettings(IgnoreApi = false)]
    [SwaggerTag("OCR")]
    [ApiController()]
    [Route("api/v2/ocr")]
    public class ApiV2OCRController : ControllerBase
    {
        static object lockObj = new object();
        static ApiV2OCRController()
        {
        }



        static bool demoswitcher = false;
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Get")]
        public async Task<ActionResult<HlidacStatu.DS.Api.OcrWork.Task>> GetTask()
        {
            CheckRoleRecord(this.User.Identity.Name);

            demoswitcher = !demoswitcher;
            if (demoswitcher)
                return new HlidacStatu.DS.Api.OcrWork.Task()
                {
                    parentDocId = "0",
                    prilohaId = "00",
                    type = HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva,
                    url = "http://zapisnikzmizeleho.cz/wp-content/themes/zapisnikz1.1/img-zapisnik/velikonocni-festival-brno-2011-04/stabat-mater-program.gif",
                    origFilename = "stabat-mater-program.gif"
                };
            else
                return new HlidacStatu.DS.Api.OcrWork.Task()
                {
                    parentDocId = "1",
                    prilohaId = "1",
                    type = HlidacStatu.DS.Api.OcrWork.DocTypes.Smlouva,
                    url = "https://www.hlidacstatu.cz/KopiePrilohy/4288264?hash=d702c02db57b151eb685c7923d673b3709710b01f1e0050add51d8d1569189a9",
                    origFilename = "DRNP2012VC1485D2.pdf"
                };

        }
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("GetMore")]
        public async Task<ActionResult<HlidacStatu.DS.Api.OcrWork.Task[]>> GetMore(int numberOfTasks)
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

            return null; //TODO

        }
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Save")]
        public async Task<ActionResult> Save([FromBody] HlidacStatu.DS.Api.OcrWork.Result data)
        {
            CheckRoleRecord(this.User.Identity.Name);

            //await HlidacStatu.Lib.Data.External.Tables.TablesInDocs.Minion.SaveFinishedTaskAsync(data);

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
                "TablesInDocsMinion",
                HttpContext?.User?.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                log);

            return StatusCode(200);
        }

        private static void CheckRoleRecord(string username)
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
                HlidacStatuApi.Code.Log.Logger.Error("cannot add {username} to the role blurredAPIAccess", e, username);
            }

        }


        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("AddTask")]
        public async Task<ActionResult<bool>> AddTask(string id, bool force = true)
        {
            return await HlidacStatu.Lib.Data.External.Tables.TablesInDocs.Minion.CreateNewTaskAsync(id, force);
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