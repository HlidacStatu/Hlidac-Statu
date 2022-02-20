using Devmasters.Enums;

using HlidacStatu.Web.Filters;

using Microsoft.AspNetCore.Mvc;

using Swashbuckle.AspNetCore.Annotations;

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

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
            return UptimeServerRepo.AllServers();
        }

        [HttpGet("domeny")]
        public ActionResult<string> Domeny()
        {
            var webs = UptimeServerRepo.AllServers()
                .ToArray()
                .Select(m=>m.HostDomain())
                .Where(m=>Uri.IsWellFormedUriString(m, UriKind.Absolute))
                .Distinct()
                ;
            return string.Join("\n", webs);
        }


        //[GZipOrDeflate()]
        [Authorize]
        [HttpGet("{id?}")]
        public ActionResult<UptimeServer.WebStatusExport> Status([FromRoute] string? id = null)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest($"Web nenalezen");

            UptimeServer host = UptimeServerRepo.Load(id);
            if (host == null)
                return BadRequest($"Web nenalezen");

            try
            {
                UptimeServer.HostAvailability data = UptimeServerRepo.AvailabilityById(host.Id, 7*24);
                UptimeSSL webssl = UptimeSSLRepo.LoadLatest(host.Id);
                var ssldata = new UptimeServer.WebStatusExport.SslData()
                {
                    Grade = webssl == null ? null : webssl.SSLGrade().ToNiceDisplayName(),
                    LatestCheck = webssl?.Created,
                    SSLExpiresAt = webssl?.CertExpiration()
                };
                if (webssl == null)
                {
                    ssldata = null;
                }
                return
                    new UptimeServer.WebStatusExport()
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
