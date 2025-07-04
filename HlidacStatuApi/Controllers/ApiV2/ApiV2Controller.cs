﻿using Amazon.Runtime.Internal.Endpoints.StandardLibrary;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatuApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nest;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;

namespace HlidacStatuApi.Controllers.ApiV2
{
    [SwaggerTag("Core")]
    [ApiController]
    [Route("api/v2")]
    public class ApiV2Controller : ControllerBase
    {
        public const int DefaultResultPageSize = 25;
        public const int MaxResultsFromES = 5000;

        private readonly Serilog.ILogger _logger = Serilog.Log.ForContext<ApiV2Controller>();
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
        [HttpPost("tracking")]
        public async Task<ActionResult> Tracking([FromBody] TrackingData data)
        {
            //possible incomming objects:
            // object 1
            // trackingData.selectedValue = value.split('÷')[0];
            // trackingData.lastQuery = autocompleteLastQuery; 
            // trackingData.source = window.location.href;
            // trackingData.type = 'partialAutocomplete'

            // object 2
            // trackingData.selectedValues = selectedValues;
            // trackingData.source = window.location.href;
            // trackingData.type = 'fullAutocomplete'

            _logger.Information("Tracking search request from: {source}, " +
                                "caller type: {type}, " +
                                "selected values: {selectedValues}, " +
                                "selected value: {selectedValue}, " +
                                "last query: {lastQuery}",
                data.Source,
                data.Type,
                data.SelectedValues,
                data.SelectedValue,
                data.LastQuery);

            return StatusCode(200);
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpGet("getexception")]
        public ActionResult<string> GetError(string exceptionText = null)
        {
            exceptionText = exceptionText ?? "test autogenerated API exception";
            throw new ApplicationException(exceptionText);
            return StatusCode(500, $"error {exceptionText}");
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpGet("geterror/{id?}")]
        public ActionResult<string> GetError([FromRoute] int? id = 200, [FromQuery] long waitSec = 0)
        {
            if (waitSec > 0)
                System.Threading.Thread.Sleep(TimeSpan.FromSeconds(waitSec));
            return StatusCode(id ?? 200, $"error {id}");
        }


        public class NotificationPayload
        {
            public string message { get; set; }
            public string to { get; set; }
        }
        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "Admin")]
        [HttpGet("notification/{id?}")]
        public async Task<ActionResult> Notification([FromRoute] string id, [FromQuery] string? message = null)
        {
            return await SendNotification(id, message);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "Admin")]
        [HttpPost("notification/{id?}")]
        [Consumes("text/plain", IsOptional = true)]
        public async Task<ActionResult> NotificationPost([FromRoute] string id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] string payload = null)
        {
            return await SendNotification(id, payload);

        }
        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "Admin")]
        [HttpPost("notification")]
        [Consumes("application/json", IsOptional = true)]
        public async Task<ActionResult> NotificationPostJson([FromBody] NotificationPayload payload = null)
        {
            return await SendNotification(payload?.to, payload?.message);

        }


        private async Task<ActionResult> SendNotification(string to, string message)
        {
            if (string.IsNullOrEmpty(to))
                throw new ArgumentNullException("to");


            if (string.IsNullOrEmpty(message))
            {
                return StatusCode(500, "text zpravy k poslani je prazdny");
            }

            try
            {
                string sender = Devmasters.Config.GetWebConfigValue("SignalSender");
                string res = "";
                var scl = new Devmasters.Comm.Signal.SimpleClient(new Uri(Devmasters.Config.GetWebConfigValue("SignalApiUrl")), sender);
                if (to.StartsWith("+"))
                    res = await scl.SendAsync(to, message);
                else if (to.ToLower() == "team")
                    res = await scl.SendToGroupNameAsync(Devmasters.Config.GetWebConfigValue("SignalTeamGroupName"), message);
                else if (to.ToLower() == "admin")
                    res = await scl.SendAsync(Devmasters.Config.GetWebConfigValue("SignalTeamAdmins").Split(';'), message);
                else
                    return StatusCode(404, $"Destination not found");
                return Ok("ok");
            }
            catch (Exception e)
            {
                return StatusCode(500, e.ToString());
            }

        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Authorize(Roles = "Admin")]
        [HttpPost("setvazbasmlouvazakazka")]
        public async Task<ActionResult> SetVazbaSmlouvaZakazka([FromBody] SmlouvaVerejnaZakazka data)
        {
            try
            {
                await SmlouvaVerejnaZakazkaRepo.Upsert(data);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error upserting smlouvaZakazka");
                return StatusCode(500, $"error {e}");
            }

            return Ok();
        }

        //[ApiExplorerSettings(IgnoreApi = true)]
        [Authorize]
        [HttpGet("getmyip")]
        public ActionResult<string> GetIp()
        {
            return HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString();
        }

        [HttpGet("Check")]
        public async Task<ActionResult> Check(int returnStatus = 200)
        {
            return StatusCode(returnStatus, $"Returned status {returnStatus}");
        }


        [Authorize]
        [HttpGet("dumps")]
        public ActionResult<DumpInfoModel[]> Dumps()
        {
            return GetDumps();
        }


        [Authorize]
        [HttpGet("dumpItems/{datatype}")]
        [Produces("application/json")]
        public ActionResult DumpItems([FromRoute] string datatype,
            [FromRoute(Name = "date")][DefaultValue("")] string? date = "null")
        {
            if (datatype.Contains("..") || datatype.Contains(Path.DirectorySeparatorChar))
            {
                _logger.Error("Wrong datatype name.");
                return StatusCode(466);
            }
            try
            {
                var bin = _dump(datatype, date);
                using (MemoryStream zipStream = new MemoryStream(bin))
                {
                    using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
                    {
                        ZipArchiveEntry firstEntry = archive.Entries.FirstOrDefault(e => !string.IsNullOrEmpty(e.Name));

                        if (firstEntry == null)
                        {
                            return NotFound("No valid files found in the ZIP archive.");
                        }

                        MemoryStream memoryStream = new MemoryStream();
                        using (Stream entryStream = firstEntry.Open())
                        {
                            entryStream.CopyTo(memoryStream);
                        }
                        memoryStream.Position = 0; // Reset stream position

                        // Return file as stream with correct content type
                        return File(memoryStream, "application/json", firstEntry.Name);
                    }
                }
            }
            catch (System.IO.FileNotFoundException)
            {
                return StatusCode(404);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }

        [Authorize]
        [HttpGet("dumpZip/{datatype}/{date?}")]
        [Produces("application/zip")]
        public ActionResult DumpZip([FromRoute] string datatype,
            [FromRoute(Name = "date")][DefaultValue("")] string? date = "null")
        {
            if (datatype.Contains("..") || datatype.Contains(Path.DirectorySeparatorChar))
            {
                _logger.Error("Wrong datatype name.");
                return StatusCode(466);
            }
            try
            {
                return File(_dump(datatype, date), "application/zip");
            }
            catch (System.IO.FileNotFoundException)
            {
                return StatusCode(404);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }

        }
        private byte[] _dump([FromRoute] string datatype,
            [FromRoute(Name = "date")][DefaultValue("")] string? date = "null")
        {

            DateTime? specificDate = Devmasters.DT.Util.ToDateTime(date, "yyyy-MM-dd");
            string onlyfile = $"{datatype}.dump" +
                              (specificDate.HasValue ? "-" + specificDate.Value.ToString("yyyy-MM-dd") : "");
            if (datatype.ToLower().StartsWith("dataset."))
                onlyfile = $"{datatype.Replace("dataset.", "dataset.hs-")}-01.dump" +
                              (specificDate.HasValue ? "-" + specificDate.Value.ToString("yyyy-MM-dd") : "");
            string fn = StaticData.Dumps_Path + $"{onlyfile}" + ".zip";
            if (System.IO.File.Exists(fn))
            {
                try
                {
                    return System.IO.File.ReadAllBytes(fn);
                }
                catch (Exception e)
                {
                    throw;
                }
            }
            else
            {
                throw new System.IO.FileNotFoundException();
            }
        }

        private static DumpInfoModel[] GetDumps()
        {
            string baseUrl = "https://api.hlidacstatu.cz/api/v2/";

            List<DumpInfoModel> data = new List<DumpInfoModel>();

            foreach (var fi in new DirectoryInfo(StaticData.Dumps_Path).GetFiles("*.zip"))
            {
                var fn = fi.Name;
                var regexStr = @"((?<type>(\w*))? \.)? (?<name>(\w|-)*)\.dump -? (?<date>\d{4} - \d{2} - \d{2})?.zip";
                DateTime? date =
                    Devmasters.DT.Util.ToDateTimeFromCode(
                        Devmasters.RegexUtil.GetRegexGroupValue(fn, regexStr, "date"));
                string name = Devmasters.RegexUtil.GetRegexGroupValue(fn, regexStr, "name");
                string nicename = name;
                if (nicename.StartsWith("hs-"))
                    nicename = nicename.Substring(3);
                if (nicename.EndsWith("-01"))
                    nicename = nicename.Substring(0, nicename.Length - 3);

                string dtype = Devmasters.RegexUtil.GetRegexGroupValue(fn, regexStr, "type");
                if (!string.IsNullOrEmpty(dtype))
                {
                    nicename = dtype + "." + nicename;
                    name = dtype + "." + name;

                }
                name = name.Replace(".dump", "").Replace(".zip", "");
                data.Add(
                    new DumpInfoModel()
                    {
                        urlJson = baseUrl + $"dumpItems/{nicename}/{date?.ToString("yyyy-MM-dd") ?? ""}",
                        urlZip = baseUrl + $"dumpZip/{nicename}/{date?.ToString("yyyy-MM-dd") ?? ""}",
                        created = fi.LastWriteTimeUtc,
                        date = date,
                        fulldump = date.HasValue == false,
                        size = fi.Length,
                        dataType = nicename
                    }
                );
                ;
            }

            return data
                .OrderByDescending(o => o.created)
                .ThenBy(o => o.dataType)
                .ToArray();
        }
    }

    public class TrackingData
    {
        public string[]? SelectedValues { get; set; }
        public string? SelectedValue { get; set; }
        public string? LastQuery { get; set; }
        public string? Source { get; set; }
        public string? Type { get; set; }
    }
}