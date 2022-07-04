using Devmasters.Enums;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using HlidacStatuApi.Code;
using HlidacStatuApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HlidacStatuApi.Controllers.ApiV2
{

    [SwaggerTag("Weby")]
    [Route("api/v2/Weby")]
    public class ApiV2WebyController : ControllerBase
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
        public ActionResult<NedostupnostModel[]> TopNedostupnost(int days)
        {

            UptimeServer.HostAvailability[] topNedostupnosti1D = Code.Availability.AllActiveServers24hoursStat()
                //.Where(m=>m.Host.Id == 108)
                .Where(o => (o.Statistics().DurationTotal.Pomale.TotalSeconds) + o.Statistics().DurationTotal.Nedostupne.TotalSeconds > 0)
                .OrderByDescending(o => (o.Statistics().DurationTotal.Pomale.TotalSeconds) + (o.Statistics().DurationTotal.Nedostupne.TotalSeconds * 5.0))
                .Take(60)
                .ToArray();
            UptimeServer.HostAvailability[] topNedostupnosti7D = Code.Availability.AllActiveServersWeekStat()
                            .Where(m => m?.Host?.Id != null)
                            .Where(o => (o.Statistics().DurationTotal.Pomale.TotalSeconds) + o.Statistics().DurationTotal.Nedostupne.TotalSeconds > 0)
                            .OrderByDescending(o => (o.Statistics().DurationTotal.Pomale.TotalSeconds) + (o.Statistics().DurationTotal.Nedostupne.TotalSeconds * 5.0))
                            .Take(60)
                            .ToArray();

            NedostupnostModel[] data = null;
            if (days < 7)
                data = topNedostupnosti1D
                    .Select(m => new NedostupnostModel() { Server = m.Host, Statistics = m.Statistics() })
                    .ToArray();
            else
                data = topNedostupnosti7D
                    .Select(m => new NedostupnostModel() { Server = m.Host, Statistics = m.Statistics() })
                    .ToArray();

            return data; ;
        }

        //[GZipOrDeflate()]
        [Authorize]
        [HttpGet("{id?}")]
        public async Task<ActionResult<UptimeServer.WebStatusExport>> Status([FromRoute] int? id = null, int? days = 1)
        {
            if (id == null)
                return BadRequest($"Web nenalezen");

            UptimeServer host = UptimeServerRepo.Load(id.Value);
            if (host == null)
                return BadRequest($"Web nenalezen");

            try
            {
                UptimeServer.HostAvailability data = null;
                if (days == null || days == 7)
                    data = Code.Availability.AvailabilityForWeekById(host.Id);
                else
                    data = Code.Availability.AvailabilityForDayById(host.Id);


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
                        SSL = ssldata,
                        IPv6Support = webssl.IP6support
                    };
            }
            catch (Exception e)
            {
                HlidacStatu.Util.Consts.Logger.Error($"_DataHost id ${id}", e);
                return BadRequest($"Interní chyba při načítání systému.");
            }
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpGet("availabilityForDayById")]
        public ActionResult<UptimeServer.HostAvailability> AvailabilityForDayById(int? id)
        {
            if (id == null)
                return BadRequest($"Web nenalezen");

            UptimeServer host = UptimeServerRepo.Load(id.Value);
            if (host == null)
                return BadRequest($"Web nenalezen");

            return Availability.AvailabilityForDayById(id.Value);
        }


        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpGet("availabilityForWeekById")]
        public ActionResult<UptimeServer.HostAvailability> AvailabilityForWeekById(int? id)
        {
            if (id == null)
                return BadRequest($"Web nenalezen");

            UptimeServer host = UptimeServerRepo.Load(id.Value);
            if (host == null)
                return BadRequest($"Web nenalezen");

            return Availability.AvailabilityForWeekById(id.Value);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("chartdata/{id}")]
        [HlidacCache(60, "id;f;t;h", false)]
        public ActionResult ChartData(string id, long? f, long? t, int? h = 24)
        {
            id = id?.ToLower() ?? "";
            string content = "{}";
            DateTime fromDate = DateTime.Now.AddHours(-1 * h.Value);
            DateTime toDate = DateTime.Now;
            if (f.HasValue)
                fromDate = new DateTime(f.Value);
            if (t.HasValue)
                toDate = new DateTime(t.Value);

            IEnumerable<UptimeServer.HostAvailability> data = null;
            if (id.StartsWith("w"))
            {
                id = id.Replace("w", "");
                var host = UptimeServerRepo.Load(Convert.ToInt32(id));
                if (host != null)
                {
                    data = new UptimeServer.HostAvailability[] { Code.Availability.AvailabilityForWeekById(host.Id) };
                }
            }
            else
                data = Code.Availability.AvailabilityForDayByGroup(id);

            if (data?.Count() > 0)
            {
                var dataArr = data.ToArray();
                for (int i = 0; i < dataArr.Length; i++)
                {
                    dataArr[i].Host.Name = Devmasters.TextUtil.ShortenText(dataArr[i].Host.Name, 40);
                }
                var dataready = new
                {
                    data = dataArr.AsEnumerable()
                      .Select((x, l) => x.DataForChart(fromDate, toDate, l))
                      //.Reverse()
                      .SelectMany(x => x)
                      .ToArray(),
                    cats = data
                        //.Reverse()
                        .ToDictionary(k => k.Host.Id, d => new { host = d.Host, lastResponse = d.Data.Last() }),
                    categories = data.Select(m => m.Host.Id)
                            //.Reverse()
                            .ToArray(),
                    colsize = data.Select(d => d.ColSize(fromDate, toDate)).Max(),
                };
                content = Newtonsoft.Json.JsonConvert.SerializeObject(dataready);
            }
            return Content(content, "application/json");
        }
    }
}
