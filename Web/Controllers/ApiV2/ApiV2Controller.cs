using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using System;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Authorization;
using MimeMapping;

namespace HlidacStatu.Web.Controllers
{
    [SwaggerTag("Core")]
    [ApiController]
    [Route("api/v2")]
    public class ApiV2Controller : ApiV2AuthController
    {
        public const int DefaultResultPageSize = 25;
        public const int MaxResultsFromES = 5000;

        /*
        Atributy pro API
        [SwaggerOperation(Tags = new[] { "Beta" })] - zarazeni metody do jine skupiny metod, pouze na urovni methody
        [ApiExplorerSettings(IgnoreApi = true)] - neni videt v dokumentaci, ani ve swagger file
        [SwaggerTag("Core")] - Tag pro vsechny metody v controller
        */


        // /api/v2/{id}
        [Authorize]
        [HttpGet("ping/{text}")]
        public ActionResult<string> Ping([FromRoute] string text)
        {
            return "pong " + text;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpGet("geterror/{id?}")]
        public ActionResult<string> GetError([FromRoute] int? id = 200)
        {
            return StatusCode(id ?? 200, $"error {id}");
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpGet("getmyip")]
        public ActionResult<string> GetIp()
        {
            return HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString();
        }

        [Authorize]
        [HttpGet("dumps")]
        public ActionResult<Models.ApiV1Models.DumpInfoModel[]> Dumps()
        {
            return Framework.Api.Dumps.GetDumps("https://www.hlidacstatu.cz/api/v2/");
        }


        [Authorize]
        [HttpGet("dump/{datatype}")]
        public ActionResult<HttpResponseMessage> Dump([FromRoute] string datatype)
        {
            return Dump(datatype, "");
        }
        [Authorize]
        [HttpGet("dump/{datatype}/{date?}")]
        public ActionResult<HttpResponseMessage> Dump([FromRoute] string datatype, [FromRoute(Name = "date")][DefaultValue("")] string? date = "null")
        {
            if (datatype.Contains("..") || datatype.Contains("\\"))
            {
                HlidacStatu.Util.Consts.Logger.Error("Wrong datatype name");
                return StatusCode(466);

            }

            DateTime? specificDate = Devmasters.DT.Util.ToDateTime(date, "yyyy-MM-dd");
            string onlyfile = $"{datatype}.dump" +
                              (specificDate.HasValue ? "-" + specificDate.Value.ToString("yyyy-MM-dd") : "");
            string fn = StaticData.Dumps_Path + $"{onlyfile}" + ".zip";

            if (System.IO.File.Exists(fn))
            {
                try
                {
                    return File(System.IO.File.ReadAllBytes(fn), "application/zip", System.IO.Path.GetFileName(fn), true);
                }
                catch (Exception e)
                {
                    HlidacStatu.Util.Consts.Logger.Error("DUMP exception?" + date, e);
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
            else
            {
                Util.Consts.Logger.Error("API DUMP : not found file " + fn);
                return NotFound($"Dump {datatype} for date:{date} nenalezen.");
            }
        }
        
    }
}