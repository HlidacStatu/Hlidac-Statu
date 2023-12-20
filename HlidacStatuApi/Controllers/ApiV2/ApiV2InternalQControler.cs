using HlidacStatu.LibCore.Extensions;
using HlidacStatu.Q.Simple.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [SwaggerTag("Voice 2 Text Old")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/v2/internalq")]
    public class ApiV2InternalQController : ControllerBase
    {
        private readonly Serilog.ILogger _logger = Serilog.Log.ForContext<ApiV2InternalQController>();
        
        /// <summary>
        ///  Vytvori novy task
        /// </summary>
        /// <param name="datasetId">id datasetu</param>
        /// <param name="itemId">id zaznamu</param>
        /// <param name="priority">0=normal; 1=fast lane; 2=express lane</param>
        /// <returns>taskid</returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpPost, Route("Voice2TextNewTask/{datasetId}/{itemId}")]
        public ActionResult<string> Voice2TextNewTask([FromRoute] string datasetId, [FromRoute] string itemId, int? priority = 0)
        {
            try
            {
                using (HlidacStatu.Q.Simple.Queue<Voice2Text> sq = new HlidacStatu.Q.Simple.Queue<Voice2Text>(
                    Voice2Text.QName_priority(priority ?? 0),
                    Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString"))
                    )
                {
                    sq.Send(new Voice2Text() { dataset = datasetId, itemid = itemId });
                    return $"OK";
                }
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Voice2TextNewTask");
                throw;
            }

        }


        /// <summary>
        /// Vrátí taskID pro Voice2Text Docker image
        /// </summary>
        /// <returns>taskid</returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpGet("Voice2TextGetTask")]
        public ActionResult<Voice2Text> Voice2TextGetTask()
        {
            Voice2Text task = null;
            foreach (var p in Voice2Text.Priorities)
            {
                try
                {

                    using (HlidacStatu.Q.Simple.Queue<Voice2Text> sq = new HlidacStatu.Q.Simple.Queue<Voice2Text>(Voice2Text.QName_priority(p),
                        Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")))
                    {
                        task = sq.GetAndAck();
                        if (task != null)
                            return task;
                    }
                }
                catch (Exception e)
                {
                    _logger.Fatal(e, "Voice2TextGetTask");
                    throw;
                }
            }
            if (task == null)
            {
                return NoContent();
            }
            return task;
        }

        /// <summary>
        /// Potvrdí ukončení Voice2Text operace
        /// </summary>
        /// <returns>taskid</returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpPost, Route("Voice2TextDone")]
        public ActionResult<string> Voice2TextDone([FromBody] Voice2Text task)
        {
            try
            {

                using (HlidacStatu.Q.Simple.Queue<TaskResult<Voice2Text>> sq
                    = new HlidacStatu.Q.Simple.Queue<TaskResult<Voice2Text>>(Voice2Text.QName_done, Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")))
                {
                    TaskResult<Voice2Text> result = new TaskResult<Voice2Text>()
                    {
                        Payload = task,
                        Created = DateTime.Now,
                        Result = "done",
                        User = HttpContext.User?.Identity?.Name,
                        FromIP = HttpContext.GetRemoteIp()
                    };
                    sq.Send(result);
                }
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Voice2TextDone");
                throw;
            }


            return $"OK";
        }

        /// <summary>
        /// Potvrdí ukončení Voice2Text operace
        /// </summary>
        /// <returns>taskid</returns>
        [Authorize(Roles = "Admin,InternalQ")]
        [HttpPost, Route("Voice2TextFailed/{requeueAsTheLast}")]
        public ActionResult<string> Voice2TextFailed([FromRoute] bool requeueAsTheLast, [FromBody] Voice2Text task)
        {
            try
            {

                using (HlidacStatu.Q.Simple.Queue<TaskResult<Voice2Text>> sq
                    = new HlidacStatu.Q.Simple.Queue<TaskResult<Voice2Text>>(Voice2Text.QName_failed, Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")))
                {
                    TaskResult<Voice2Text> result = new TaskResult<Voice2Text>()
                    {
                        Payload = task,
                        Created = DateTime.Now,
                        Result = "failed",
                        User = HttpContext.User?.Identity?.Name,
                        FromIP = HttpContext.GetRemoteIp()
                    };
                    sq.Send(result);
                }
            }
            catch (Exception e)
            {
                _logger.Fatal(e, "Voice2TextFailed");
                throw;
            }

            return "OK";
        }
    }

}
