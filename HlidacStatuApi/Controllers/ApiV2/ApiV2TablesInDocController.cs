using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Data;
using Microsoft.Data.SqlClient;
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

            var q = await QTblsInDocRepo.GetNextToProcessAsync();
            if (q == null)
                return StatusCode(404);

            return q.ToTablesInDocTask();

        }

        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("GetMore")]
        public async Task<ActionResult<HlidacStatu.DS.Api.TablesInDoc.Task[]>> GetMore(int numberOfTasks)
        {

            HlidacStatu.DS.Api.TablesInDoc.Task[] tasks = (await QTblsInDocRepo.GetNextToProcessAsync(numberOfTasks))
                .Select(m=>m.ToTablesInDocTask())
                .ToArray();

            return tasks;

        }
        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Save")]
        public async Task<ActionResult> Save([FromBody] HlidacStatu.DS.Api.TablesInDoc.ApiResult2 data)
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(QTblsInDocRepo.SetDoneAsync(data.task.smlouvaId, data.task.prilohaId));
            if (data?.results != null)
            {
                HlidacStatu.DS.Api.TablesInDoc.Result[] tables = data.results;
                tasks.Add( DocTablesRepo.SaveAsync(data.task.smlouvaId, data.task.prilohaId, tables));
            }
            Task.WaitAll(tasks.ToArray());
            return StatusCode(200);
        }


        [ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpPost("Log")]
        public async Task<ActionResult> Log([FromBody] string log)
        {

            _logger.Error(
                "{action} {from} {user} {ip} {message}",
                "RemoteLog",
                "TablesInDocsMinion",
                HttpContext?.User?.Identity?.Name,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                log);

            return StatusCode(200);
        }


        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("AddTask")]
        public async Task<ActionResult<bool>> AddTask(string id, bool force = true)
        {
            return await HlidacStatu.Repositories.QTblsInDocRepo.CreateNewTaskAsync(id, force);
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        //[Authorize(Roles = "blurredAPIAccess")]
        [HttpGet("Stats")]
        public async Task<ActionResult<BlurredPageAPIStatistics>> Stats()
        {
            
            DateTime now = DateTime.Now;
            var res = new BlurredPageAPIStatistics()
            {
                total = -1
            };

            return res;
        }
    }
}