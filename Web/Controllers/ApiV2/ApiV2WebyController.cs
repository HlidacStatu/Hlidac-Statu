using Devmasters.Enums;

using HlidacStatu.Lib.Data.External.Zabbix;
using HlidacStatu.Web.Filters;

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

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
        public ActionResult<ZabHost[]> List()
        {
            return ZabTools.Weby().ToArray();
        }

        [HttpGet("domeny")]
        public ActionResult<string> Domeny()
        {
            var webs = ZabTools.Weby()
                .ToArray()
                .Select(m=>m.url)
                .Where(m=>Uri.IsWellFormedUriString(m, UriKind.Absolute))
                .Select(m=>new Uri(m))
                .Select(m=>m.Host.ToLower())
                .Distinct()
                ;
            return string.Join("\n", webs);
        }


        //[GZipOrDeflate()]
        [Authorize]
        [HttpGet("{id?}")]
        public ActionResult<WebStatusExport> Status([FromRoute] string? id = null)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest($"Web nenalezen");

            ZabHost host = ZabTools.Weby().Where(w => w.hostid == id.ToString() & w.itemIdResponseTime != null).FirstOrDefault();
            if (host == null)
                return BadRequest($"Web nenalezen");

            try
            {
                ZabHostAvailability data = ZabTools.GetHostAvailabilityLong(host);
                ZabHostSslStatus webssl = ZabTools.SslStatusForHostId(host.hostid);
                var ssldata = new WebStatusExport.SslData()
                {
                    Grade = webssl?.Status().ToNiceDisplayName(),
                    LatestCheck = webssl?.Time,
                    Copyright = "(c) © Qualys, Inc. https://www.ssllabs.com/",
                    FullReport = "https://www.ssllabs.com/ssltest/analyze.html?d=" + webssl?.Host?.UriHost()
                };
                if (webssl == null)
                {
                    ssldata = null;
                }
                return
                    new WebStatusExport()
                    {
                        Availability = data,
                        SSL = ssldata
                    };

            }
            catch (Exception e)
            {
                Util.Consts.Logger.Error($"_DataHost id ${id}", e);
                return BadRequest($"Interní chyba při načítání systému.");
            }
        }

    }
}
