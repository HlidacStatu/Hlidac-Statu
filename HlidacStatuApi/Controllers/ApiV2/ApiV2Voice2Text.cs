using HlidacStatu.DS.Api.Voice2Text;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RazorEngine.Compilation.ImpromptuInterface;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [SwaggerTag("Voice 2 Text")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/v2/voice2text")]
    public class ApiV2Voice2TextController : ControllerBase
    {
        /// <summary>
        /// Vytvori task a ulozi do fronty
        /// </summary>
        /// <param name="callerId">ID volajiciho</param>
        /// <param name="callerTaskId">Interni ID pozadavku volajiciho</param>
        /// <param name="source">URL nebo soubor co se zpracovat</param>
        /// <param name="priority">priorita</param>
        /// <param name="datasetName"></param>
        /// <param name="dataSetItemId"></param>
        /// <param name="deleteAfterProcess"></param>
        /// <returns></returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpPost("CreateTask")]
        public async Task<ActionResult<long>> CreateTask([FromBody] HlidacStatu.DS.Api.Voice2Text.Task task)
        {
            var qv2t = new QVoiceToText();
            qv2t.Priority = task.Priority;
            qv2t.Source = task.Source;
            if (task.SourceOptions?.datasetName!= null || task.SourceOptions?.deleteFileAfterProcess == true)
                qv2t.SetSourceOptions<HlidacStatu.DS.Api.Voice2Text.Options>(task.SourceOptions);

            qv2t.CallerId = task.CallerId;
            qv2t.CallerTaskId = task.CallerTaskId;
            await QVoiceToTextRepo.SaveAsync(qv2t);
            return qv2t.QId;
        }
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpGet("Check")]
        public async Task<ActionResult<string>> Check()
        {
            return "OK";
        }

        /// <summary>
        /// Vrátí taskID pro Voice2Text Docker image
        /// </summary>
        /// <returns>taskid</returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpGet("GetWaitingTask")]
        public async Task<ActionResult<HlidacStatu.DS.Api.Voice2Text.Task>> GetWaitingTask()
        {
            var q = await QVoiceToTextRepo.GetNextToProcess();


            if (q == null)
            {
                return NotFound();
            }
            return new HlidacStatu.DS.Api.Voice2Text.Task()
            {
                QId = q.QId,
                Created = q.Created,
                Started = q.Started,
                Source = q.Source,
                SourceOptions = q.GetSourceOptions<HlidacStatu.DS.Api.Voice2Text.Options>()
            };
        }

        /// <summary>
        /// Potvrdí ukončení Voice2Text operace
        /// </summary>
        /// <returns>taskid</returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpPost("TaskDone")]
        public async Task<ActionResult<string>> TaskDone([FromBody] HlidacStatu.DS.Api.Voice2Text.Task task)
        {
            var q = await QVoiceToTextRepo.Finish(task.QId, task.Result, task.Status);
            if (q == null)
                return "Not found";
            else
                return $"OK";
        }

    }

}
