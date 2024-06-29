using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [SwaggerTag("Voice 2 Text")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [Route("api/v2/voice2text")]
    public class ApiV2Voice2TextController : ControllerBase
    {
        private readonly Serilog.ILogger _logger = Serilog.Log.ForContext<ApiV2FirmyController>();

        /// <summary>
        /// Vytvori task a ulozi do fronty. Vrati id tasku
        /// </summary>
        /// <param name="task">Cislo id taasku</param>
        /// <returns></returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpPost("CreateTask")]
        public async Task<ActionResult<long?>> CreateTask([FromBody] HlidacStatu.DS.Api.Voice2Text.Task task, 
            [FromQuery] bool addDuplicated = false)
        {
            try
            {
                //check duplication
                if (addDuplicated == false && (await QVoiceToTextRepo.IsDuplicatedBySource(task.Source)))
                    return StatusCode(204);
                

                var qv2t = new QVoiceToText();
                qv2t.Priority = task.Priority;
                qv2t.Source = task.Source;
                if (task.SourceOptions?.datasetName != null || task.SourceOptions?.deleteFileAfterProcess == true)
                    qv2t.SetSourceOptions<HlidacStatu.DS.Api.Voice2Text.Options>(task.SourceOptions);

                qv2t.CallerId = task.CallerId;
                qv2t.CallerTaskId = task.CallerTaskId;
                qv2t.Status = (int)HlidacStatu.DS.Api.Voice2Text.Task.CheckState.WaitingInQueue;
                var saved = await QVoiceToTextRepo.SaveAsync(qv2t);
                return saved.QId;
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }


        [Authorize(Roles = "Admin,InternalQ")]
        [HttpGet("Check")]
        public async Task<ActionResult> Check(int returnStatus = 200)
        {
            return StatusCode(returnStatus, $"Returned status {returnStatus}");
        }

        /// <summary>
        /// Vrátí taskID pro Voice2Text Docker image
        /// </summary>
        /// <returns>taskid</returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpGet("GetNextTask")]
        public async Task<ActionResult<HlidacStatu.DS.Api.Voice2Text.Task>> GetNextTask([FromQuery] string processEngine)
        {
            var q = await QVoiceToTextRepo.GetNextToProcess(processEngine);


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
        public async Task<ActionResult> TaskDone([FromBody] HlidacStatu.DS.Api.Voice2Text.Task task)
        {
            try
            {
                string strResult = System.Text.Json.JsonSerializer.Serialize(task.Result);

                var q = await QVoiceToTextRepo.Finish(task.QId, strResult, task.Status);
                if (q == null)
                    return StatusCode(404);
                else
                    return StatusCode(200);

            }
            catch (Exception e)
            {
                return StatusCode(500, $"task {task.QId} : {e.Message}");
            }
        }

        [Authorize(Roles = "Admin,InternalQ")]
        [HttpGet("SetTaskStatus")]
        public async Task<ActionResult> SetTaskStatus([FromQuery] long qId, [FromQuery] HlidacStatu.DS.Api.Voice2Text.Task.CheckState status)
        {
            try
            {
                var q = await QVoiceToTextRepo.SetStatus(qId, status);
                if (q == null)
                    return StatusCode(404);
                else
                    return StatusCode(200);

            }
            catch (Exception e)
            {
                return StatusCode(500, $"task {qId} : {e.Message}");
            }
        }

        [Authorize(Roles = "Admin,InternalQ")]
        [HttpGet("RestartTask")]
        public async Task<ActionResult<string>> RestartTask([FromQuery] long qId, [FromQuery] int? withPriority = null)
        {
            try
            {
                var q = await QVoiceToTextRepo.GetOnlySpecific(qId);
                if (q == null)
                    return StatusCode(404);
                else
                {
                    var nQ = new QVoiceToText();
                    nQ.CallerId = q.CallerId;
                    nQ.CallerTaskId = q.CallerTaskId;
                    nQ.Priority = withPriority ?? q.Priority;
                    nQ.Source = q.Source;
                    nQ.SourceOptionsRaw = q.SourceOptionsRaw;
                    nQ.Status = (int)HlidacStatu.DS.Api.Voice2Text.Task.CheckState.WaitingInQueue;
                    var saved = await QVoiceToTextRepo.SaveAsync(nQ);
                    return saved.QId.ToString();
                }
            }
            catch (Exception e)
            {
                return StatusCode(500, $"task {qId} : {e.Message}");
            }
        }
        static System.Text.Json.JsonSerializerOptions jsonSerOpt = new System.Text.Json.JsonSerializerOptions() { PropertyNameCaseInsensitive = true };

        [Authorize(Roles = "Admin,InternalQ")]
        [HttpGet("GetTasks")]
        public async Task<ActionResult<HlidacStatu.DS.Api.Voice2Text.Task[]>> GetTasks(
            [FromQuery] int maxItems, [FromQuery] string? callerId, [FromQuery] string? callerTaskId = null, [FromQuery] HlidacStatu.DS.Api.Voice2Text.Task.CheckState? status = null)
        {

            if (maxItems > 50000)
                maxItems = 50000;
            if (maxItems < 1)
                maxItems = 100;
            try
            {

                QVoiceToText[] tasks = await QVoiceToTextRepo.GetByParameters(maxItems, callerId, callerTaskId, status: status);
                var res = new List<HlidacStatu.DS.Api.Voice2Text.Task>();
                foreach (var m in tasks) {
                    try
                    {
                        var r = new HlidacStatu.DS.Api.Voice2Text.Task()
                        {
                            CallerId = m.CallerId,
                            CallerTaskId = m.CallerTaskId,
                            Created = m.Created,
                            Done = m.Done,
                            Priority = m.Priority ?? 1,
                            QId = m.QId,
                            Result = System.Text.Json.JsonSerializer.Deserialize<Devmasters.SpeechToText.Term[]>(m.Result, jsonSerOpt),
                            Source = m.Source,
                            SourceOptions = m.GetSourceOptions<HlidacStatu.DS.Api.Voice2Text.Options>(),
                            Started = m.Started,
                            Status = ((HlidacStatu.DS.Api.Voice2Text.Task.CheckState)(m.Status ?? 0))

                        };
                        res.Add(r);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Cannot deserialize task result for {QiD}", m.QId);
                    }
                }                    

                return res.ToArray();
            }
            catch (Exception e)
            {
                _logger.Error(e, "GetTasksByParameters error, parameters maxItems:{maxItems};"
                    + $"callerId:{callerId};callerTaskId:{callerTaskId};status:{status}. Error {e.Message}");
                throw;
            }

        }

    }
}
