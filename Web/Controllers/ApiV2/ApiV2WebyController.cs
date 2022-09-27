using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatu.Web.Controllers
{

    [SwaggerTag("Weby")]
    [Route("api/v2/Weby")]
    public class ApiV2WebyController : ApiV2AuthController
    {

        /*
        Atributy pro API
        [SwaggerOperation(Tags = new[] { "Beta" })] - zarazeni metody do jine skupiny metod, pouze na urovni methody
        [ApiExplorerSettings(IgnoreApi = true)] - neni videt v dokumentaci, ani ve swagger file
        [SwaggerTag("Core")] - Tag pro vsechny metody v controller
        */


        // /api/v2/{id}
        //[GZipOrDeflate()]
        [Authorize]
        [HttpGet]
        public ActionResult<UptimeServer[]> List()
        {
            return UptimeServerRepo.AllActiveServers();
        }

        [HttpGet("domeny")]
        public ActionResult<string> Domeny()
        {
            var webs = UptimeServerRepo.AllActiveServers()
                .ToArray()
                .Select(m => m.HostDomain())
                .Where(m => string.IsNullOrEmpty(m) == false)
                .Distinct()
                ;
            return string.Join("\n", webs);
        }


        [Authorize]
        [HttpGet("nedostupnost")]
        public async Task<ActionResult<Models.Apiv2.NedostupnostModel[]>> TopNedostupnost(int days)
        {
            try
            {
                var res = await Devmasters.Net.HttpClient.Simple.GetAsync(
                    Framework.Constants.ApiURL + "api/v2/weby/nedostupnost?days=" + days,
                    timeout: TimeSpan.FromSeconds(120),
                    headers: new Dictionary<string, string> { { "Authorization", HlidacStatu.Web.Framework.Constants.ApiToken } }
                    );

                return Content(res, "application/json", System.Text.Encoding.UTF8);

            }
            catch (Exception e)
            {
                Util.Consts.Logger.Error($"TopNEdostupnost ${days}", e);
                return BadRequest($"Interní chyba při načítání systému.");
            }
        }

        //[GZipOrDeflate()]
        [Authorize]
        [HttpGet("{id?}")]
        public async Task<ActionResult<UptimeServer.WebStatusExport>> Status([FromRoute] int? id = null)
        {
            if (id == null)
                return BadRequest($"Web nenalezen");

            UptimeServer host = UptimeServerRepo.Load(id.Value);
            if (host == null)
                return BadRequest($"Web nenalezen");

            try
            {
                var res = await Devmasters.Net.HttpClient.Simple.GetAsync(Framework.Constants.ApiURL + $"api/v2/weby/{id}",
                    timeout: TimeSpan.FromSeconds(10),
                    headers: new Dictionary<string, string> { { "Authorization", HlidacStatu.Web.Framework.Constants.ApiToken } }
                    );

                return Content(res, "application/json", System.Text.Encoding.UTF8);

            }
            catch (Exception e)
            {
                Util.Consts.Logger.Error($"_DataHost id ${id}", e);
                return BadRequest($"Interní chyba při načítání systému.");
            }
        }

    }
}
