using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [SwaggerTag("AITask")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [Route("api/v2/aitask")]
    public class ApiV2AITaskController : ControllerBase
    {
        private readonly Serilog.ILogger _logger = Serilog.Log.ForContext<ApiV2AITaskController>();

        /// <summary>
        /// Vytvori task a ulozi do fronty. Vrati id tasku
        /// </summary>
        /// <param name="task">Cislo id tasku</param>
        /// <returns></returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpPost("CreateTask")]
        public async Task<ActionResult<long?>> CreateTask([FromBody] HlidacStatu.DS.Api.AITask.Task task,
            [FromQuery] bool addDuplicated = false)
        {
            try
            {
                //check duplication
                var aiT = new QAITask();
                aiT.Priority = task.Priority;
                aiT.Source = task.Source;
                if (task.Options != null)
                    aiT.SetOptions<HlidacStatu.DS.Api.AITask.Options>(task.Options);

                aiT.CallerId = task.CallerId;
                aiT.CallerTaskId = task.CallerTaskId;
                aiT.CallerTaskType = task.CallerTaskType;
                aiT.Status = (int)HlidacStatu.DS.Api.AITask.Task.CheckState.WaitingInQueue;

                var saved = await QAITaskRepo.CreateNewAsync(aiT);
                if (saved.Item1)
                    return saved.Item2.QId;
                else
                    return null;
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
        public async Task<ActionResult<HlidacStatu.DS.Api.AITask.Task>> GetNextTask(
            [FromQuery] string processEngine,
            [FromQuery] string filterByCallerId = null,
            [FromQuery] string filterByCallerTaskId = null,
            [FromQuery] string filterByCallerTaskType = null
            )
        {
            var q = await QAITaskRepo.GetNextToProcess(processEngine, filterByCallerId, filterByCallerTaskId, filterByCallerTaskType);


            if (q == null)
            {
                return NotFound();
            }
            return new HlidacStatu.DS.Api.AITask.Task()
            {
                QId = q.QId,
                Created = q.Created,
                Started = q.Started,
                Source = q.Source,
                CallerId = q.CallerId,
                CallerTaskId = q.CallerTaskId,
                CallerTaskType = q.CallerTaskType,
                Options = q.GetOptions<HlidacStatu.DS.Api.AITask.Options>()
            };
        }

        /// <summary>
        /// Potvrdí ukončení Voice2Text operace
        /// </summary>
        /// <returns>taskid</returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpPost("TaskDone")]
        public async Task<ActionResult> TaskDone([FromBody] HlidacStatu.DS.Api.AITask.Task task)
        {
            try
            {

                var q = await QAITaskRepo.Finish(task);
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
        public async Task<ActionResult> SetTaskStatus([FromQuery] long qId, [FromQuery] HlidacStatu.DS.Api.AITask.Task.CheckState status)
        {
            try
            {
                var q = await QAITaskRepo.SetStatus(qId, status);
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
                var q = await QAITaskRepo.GetOnlySpecific(qId);
                if (q == null)
                    return StatusCode(404);
                else
                {
                    var nQ = new QAITask();
                    nQ.CallerId = q.CallerId;
                    nQ.CallerTaskId = q.CallerTaskId;
                    nQ.CallerTaskType = q.CallerTaskType;
                    nQ.Priority = withPriority ?? q.Priority;
                    nQ.Source = q.Source;
                    nQ.OptionsRaw = q.OptionsRaw;
                    nQ.Status = (int)HlidacStatu.DS.Api.AITask.Task.CheckState.WaitingInQueue;
                    var saved = await QAITaskRepo.SaveExistingAsync(nQ);
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
        public async Task<ActionResult<HlidacStatu.DS.Api.AITask.Task[]>> GetTasks(
            [FromQuery] int maxItems, [FromQuery] string? callerId, 
            [FromQuery] string? callerTaskId = null, 
            [FromQuery] HlidacStatu.DS.Api.AITask.Task.CheckState? status = null)
        {

            if (maxItems > 50000)
                maxItems = 50000;
            if (maxItems < 1)
                maxItems = 100;
            try
            {

                QAITask[] tasks = await QAITaskRepo.GetByParameters(maxItems, callerId, callerTaskId, status: status);
                var res = new List<HlidacStatu.DS.Api.AITask.Task>();
                foreach (var m in tasks)
                {
                    try
                    {
                        var r = new HlidacStatu.DS.Api.AITask.Task()
                        {
                            CallerId = m.CallerId,
                            CallerTaskId = m.CallerTaskId,
                            Created = m.Created,
                            Done = m.Done,
                            Priority = m.Priority ?? 1,
                            QId = m.QId,
                            ResultSerialized = m.Result,
                            Source = m.Source,
                            Options = m.GetOptions<HlidacStatu.DS.Api.AITask.Options>(),
                            Started = m.Started,
                            Status = ((HlidacStatu.DS.Api.AITask.Task.CheckState)(m.Status ?? 0))

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
