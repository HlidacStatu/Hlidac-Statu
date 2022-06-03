using System;
using System.Linq;
using System.Threading.Tasks;

using Devmasters.Enums;

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
        public ActionResult<Models.Apiv2.NedostupnostModel[]> TopNedostupnost(int days)
        {

            UptimeServer.HostAvailability[] topNedostupnosti1D = HlidacStatu.Repositories.UptimeServerRepo.AllActiveServers24hoursStat()
                //.Where(m=>m.Host.Id == 108)
                .Where(o => (o.Statistics().DurationTotal.Pomale.TotalSeconds) + o.Statistics().DurationTotal.Nedostupne.TotalSeconds > 0)
                .OrderByDescending(o => (o.Statistics().DurationTotal.Pomale.TotalSeconds) + (o.Statistics().DurationTotal.Nedostupne.TotalSeconds * 5.0))
                .Take(60)
                .ToArray();
            UptimeServer.HostAvailability[] topNedostupnosti7D = HlidacStatu.Repositories.UptimeServerRepo.AllActiveServersWeekStat()
                            .Where(m => m?.Host?.Id != null)
                            .Where(o => (o.Statistics().DurationTotal.Pomale.TotalSeconds) + o.Statistics().DurationTotal.Nedostupne.TotalSeconds > 0)
                            .OrderByDescending(o => (o.Statistics().DurationTotal.Pomale.TotalSeconds) + (o.Statistics().DurationTotal.Nedostupne.TotalSeconds * 5.0))
                            .Take(60)
                            .ToArray();

            Models.Apiv2.NedostupnostModel[] data = null;
            if (days < 7)
                data = topNedostupnosti1D
                    .Select(m => new Models.Apiv2.NedostupnostModel() { Server = m.Host, Statistics = m.Statistics() })
                    .ToArray();
            else
                data = topNedostupnosti7D
                    .Select(m => new Models.Apiv2.NedostupnostModel() { Server = m.Host, Statistics = m.Statistics() })
                    .ToArray();

            return data; ;
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
                UptimeServer.HostAvailability data = UptimeServerRepo.AvailabilityForWeekById(host.Id);
                UptimeSSL webssl = await UptimeSSLRepo.LoadLatestAsync(host.HostDomain());
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
