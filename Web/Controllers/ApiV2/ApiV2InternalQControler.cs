using System;
using System.Net;
using HlidacStatu.Q.Simple.Tasks;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Models.Apiv2;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatu.Web.Controllers
{

    [SwaggerTag("Voice 2 Text")]
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/v2/internalq")]
    public class ApiV2InternalQController : ApiV2AuthController
    {
        /// <summary>
        ///  Vytvori novy task
        /// </summary>
        /// <param name="datasetId">id datasetu</param>
        /// <param name="itemId">id zaznamu</param>
        /// <param name="priority">0=normal; 1=fast lane; 2=express lane</param>
        /// <returns>taskid</returns>
        [AuthorizeAndAudit(Roles = "Admin,internalQ")]
        [HttpPost, Route("Voice2TextNewTask/{datasetId}/{itemId}")]
        public ActionResult<string> Voice2TextNewTask([FromRoute]string datasetId, [FromRoute]string itemId, int priority=0)
        {
            using (HlidacStatu.Q.Simple.Queue<Voice2Text> sq = new Q.Simple.Queue<Voice2Text>(
                Voice2Text.QName_priority(priority),
                Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString"))
                )
            {
                sq.Send(new Voice2Text() { dataset = datasetId, itemid = itemId });
                return $"OK";
            }
        }


        /// <summary>
        /// Vrátí taskID pro Voice2Text Docker image
        /// </summary>
        /// <returns>taskid</returns>
        [AuthorizeAndAudit(Roles = "Admin,internalQ")]
        [HttpGet("Voice2TextGetTask")]
        public ActionResult<Voice2Text> Voice2TextGetTask()
        {
            Voice2Text task = null;
            foreach (var p in Voice2Text.Priorities)
            {

                using (HlidacStatu.Q.Simple.Queue<Voice2Text> sq = new Q.Simple.Queue<Voice2Text>(Voice2Text.QName_priority(p),
                    Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")))
                {
                    task = sq.GetAndAck();
                    if (task != null)
                        return task;
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
        [AuthorizeAndAudit(Roles = "Admin,internalQ")]
        [HttpPost, Route("Voice2TextDone")]
        public ActionResult<string> Voice2TextDone([FromBody] Voice2Text task)
        {
            using (HlidacStatu.Q.Simple.Queue<TaskResult<Voice2Text>> sq
                = new Q.Simple.Queue<TaskResult<Voice2Text>>(Voice2Text.QName_done, Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")))
            {
                TaskResult<Voice2Text> result = new TaskResult<Voice2Text>()
                {
                    Payload = task,
                    Created = DateTime.Now,
                    Result = "done",
                    User = this.ApiAuth?.ApiCall?.User,
                    FromIP = this.HostIpAddress
                };
                sq.Send(result);
            }


            return $"OK";
        }

        /// <summary>
        /// Potvrdí ukončení Voice2Text operace
        /// </summary>
        /// <returns>taskid</returns>
        [AuthorizeAndAudit(Roles = "Admin,internalQ")]
        [HttpPost, Route("Voice2TextFailed/{requeueAsTheLast}")]
        public ActionResult<string> Voice2TextFailed([FromRoute]bool requeueAsTheLast, [FromBody] Voice2Text task)
        {
            using (HlidacStatu.Q.Simple.Queue<TaskResult<Voice2Text>> sq
                = new Q.Simple.Queue<TaskResult<Voice2Text>>(Voice2Text.QName_failed, Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString")))
            {
                TaskResult<Voice2Text> result = new TaskResult<Voice2Text>()
                {
                    Payload = task,
                    Created = DateTime.Now,
                    Result = "failed",
                    User = this.ApiAuth?.ApiCall?.User,
                    FromIP = this.HostIpAddress
                };
                sq.Send(result);
            }

            return "OK";
        }
    }

}
