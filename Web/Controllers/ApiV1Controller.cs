using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Repositories.ProfilZadavatelu;
using HlidacStatu.Web.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Nest;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.LibCore;
using HlidacStatu.Web.Framework;

namespace HlidacStatu.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("api/v1/{action}/{_id?}/{_dataid?}")]
    public partial class ApiV1Controller : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ApiV1Controller(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public ActionResult UX()
        {
            return View();
        }

        public ActionResult Doc()
        {
            return View();
        }


        public ActionResult OcrStat()
        {
            return View();
        }

        [Filters.HlidacCache(60, "", false)]
        public ActionResult BpStat()
        {
            return View();
        }

        [Authorize(Roles ="Admin")]
        public ActionResult BpStat2()
        {
            return View();
        }

        // GET: ApiV1
        [Authorize]
        public ActionResult Index()
        {
            
            //global::hlst
            ViewBag.Token = AspNetUserApiToken.GetToken(User.Identity.Name).Token
                .ToString("N");

            if (!string.IsNullOrEmpty(Request.Query["getocr"]))
            {
                using (Devmasters.Net.HttpClient.URLContent url = new(
                    $"https://ocr.hlidacstatu.cz/AddApi.ashx?apikey={Devmasters.Config.GetWebConfigValue("OCRServerApiKey")}&email={User.Identity.Name}"
                ))
                {
                    var json = Newtonsoft.Json.Linq.JToken.Parse(url.GetContent().Text);

                    ViewBag.OcrToken = json.Value<string>("apikey");
                }
            }

            return View();
            
        }

        [Authorize]
        public async Task<ActionResult> ResendConfirmationMail(string _id)
        {
            string? id = _id;

            var userEmail = HttpContext.User.Identity.Name;

            using (DbEntities db = new())
            {
                if (string.IsNullOrEmpty(id))
                {
                    id = db.Users.AsNoTracking()
                        .FirstOrDefault(m => m.Email == userEmail)?.Id;
                }

                var users = db.Users.AsNoTracking()
                    .Where(m => m.EmailConfirmed == false);

                if (!string.IsNullOrEmpty(id) && id != "*")
                    users = users.Where(m => m.Id == id);


                foreach (var user in users)
                {
                    string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.Page("ConfirmEmail", "Account", new { userId = user.Id, code = code },
                        protocol: Request.Scheme);
                    //create email
                    var email = XLib.Emails.EmailMsg.CreateEmailMsgFromPostalTemplate("ConfirmEmail");
                    email.Model.CallbackUrl = callbackUrl;
                    email.To = user.Email;
                    email.SendMe();
                }
            }

            return Content("ok");
        }


        [Obsolete()]
        [Authorize]
        public ActionResult Dumps()
        {
            return Content(
                Newtonsoft.Json.JsonConvert.SerializeObject(Framework.Api.Dumps.GetDumps(),
                    Newtonsoft.Json.Formatting.Indented), "application/json");
        }

        public ActionResult OCRStats(string type = "")
        {
            string cnnStr = Devmasters.Config.GetWebConfigValue("OldEFSqlConnection");
            string sql = @"select 'Celkem' as 'type',
		            (select count(*) from ItemToOcrQueue with (nolock) where started is null) as waiting,
		            (select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is null) as running,
		            (select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is not null and done > DATEADD(dy,-1,getdate())) as doneIn24H,
		            (select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is null and started< dateadd(hh,-24,getdate())) as errors
                union
	            select distinct t.itemtype as 'type',
		            (select count(*) from ItemToOcrQueue with (nolock) where started is null and itemtype = t.itemtype) as waiting,
		            (select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is null and itemtype = t.itemtype) as running,
		            (select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is not null 
		            and done > DATEADD(dy,-1,getdate()) and itemtype = t.itemtype) as doneIn24H,
		            (select count(*) from ItemToOcrQueue with (nolock) where started is not null and done is null 
		            and started< dateadd(hh,-24,getdate()) and itemtype = t.itemtype) as errors
		            from ItemToOcrQueue t with (nolock)
		            order by type";

            using (var p = new Devmasters.PersistLib())
            {
                var ds = p.ExecuteDataset(cnnStr, System.Data.CommandType.Text, sql, null);
                System.Text.StringBuilder sb = new(1024);
                sb.AppendLine("Typ\tVe frontě\tZpracovavane\tHotovo za 24hod\tChyby pri zpracovani");
                foreach (System.Data.DataRow dr in ds.Tables[0].Rows)
                {
                    sb.Append((string)dr[0]);
                    sb.Append("\t");
                    sb.Append((int)dr[1]);
                    sb.Append("\t");
                    sb.Append((int)dr[2]);
                    sb.Append("\t");
                    sb.Append((int)dr[3]);
                    sb.Append("\t");
                    sb.Append((int)dr[4]);
                    sb.AppendLine();
                }

                return Content(sb.ToString());
            }
        }

        [Obsolete()]
        [Authorize]
        public ActionResult Dump(string date, string datatype = "smlouvy")
        {
            Util.Consts.Logger.Info(new Devmasters.Log.LogMessage()
                .SetMessage("Downloading smlouvy.dump.zip")
                .SetCustomKeyValue("UserId", User.Identity.Name)
            );

            DateTime? specificDate = Devmasters.DT.Util.ToDateTime(date, "yyyy-MM-dd");
            string onlyfile = $"{datatype}.dump" +
                              (specificDate.HasValue ? "-" + specificDate.Value.ToString("yyyy-MM-dd") : "");
            string fn = StaticData.Dumps_Path + $"{onlyfile}" + ".zip";

            if (System.IO.File.Exists(fn))
            {
                long FileL = (new FileInfo(fn)).Length;
                byte[] bytes = new byte[1024 * 1024];
                Response.Clear();
                Response.ContentType = "application/octet-stream";
                Response.Headers.Add("content-disposition",
                    "attachment; filename=" + Path.GetFileName(fn));
                //Response.AddHeader("Content-Length", FileL.ToString());
                try
                {
                    using (FileStream FS = System.IO.File.OpenRead(fn))
                    {
                        int bytesRead = 0;
                        while ((bytesRead = FS.Read(bytes, 0, bytes.Length)) > 0)
                        {
                            Response.Body.Write(bytes, 0, bytesRead);
                            Response.Body.Flush();
                        }

                        Response.Body.Flush();
                    }

                    //Response.Close();
                    Response.Body.Flush();
                    System.Threading.Thread.Sleep(1000);
                    //Response.End();
                }
                catch (ConnectionAbortedException wex)
                {
                    if (wex.Message.StartsWith("The remote host closed the connection"))
                    {
                        //ignore
                    }
                    else
                        Util.Consts.Logger.Error("DUMP?" + date, wex);
                }

                return new EmptyResult();
                //return File(fn, "application/zip");
            }
            else
            {
                Util.Consts.Logger.Error("API DUMP : not found file " + fn);
                return new NotFoundResult();
            }
        }

        class VZProfilesListRes
        {
            public string profileId { get; set; }
            public string url { get; set; }
            public long? count { get; set; }
        }

        //todo: k čemu se používá? Kandidát na promazání
        [Authorize]
        public async Task<ActionResult> VZProfilesList()
        {
            
            List<VZProfilesListRes> list = new();
            var client = await Manager.GetESClient_VerejneZakazkyNaProfiluRawAsync();
            var res = await client.SearchAsync<ZakazkaRaw>(s => s
                    .Query(q => q.Bool(b => b.MustNot(mn => mn.Term(t => t.Field(f => f.Converted).Value(1)))))
                    .Size(0)
                    .Aggregations(agg => agg
                        .Terms("profiles", t => t.Field("profil")
                            .Size(250)
                            .Order(o => o.CountDescending())
                        )
                    )
                );

            if (res.IsValid)
            {
                foreach (KeyedBucket<object> val in ((BucketAggregate)res.Aggregations["profiles"]).Items)
                {
                    var vzClient = await Manager.GetESClient_VerejneZakazkyAsync();
                    var resProf = await client.GetAsync<ProfilZadavatele>((string)val.Key);
                    list.Add(new VZProfilesListRes()
                    { profileId = (string)val.Key, url = resProf?.Source?.Url, count = val.DocCount });
                }
            }

            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(list.ToArray()), "application/json");
        
        }

        [Authorize]
        public async Task<ActionResult> VZList(string _id)
        {
            string id = _id;

            if (string.IsNullOrEmpty(id))
                return new NotFoundResult();

            var client = await Manager.GetESClient_VerejneZakazkyNaProfiluRawAsync();
            var res = await client.SearchAsync<ZakazkaRaw>(s => s
                    .Query(q => q
                        .QueryString(qs =>
                            qs.DefaultOperator(Operator.And).Query("NOT(converted:1) AND profil:\"" + id + "\""))
                    )
                    .Size(50)
                );

            if (res.IsValid)
            {
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                    res.Hits.Select(m => m.Source).ToArray()
                ), "application/json");
            }

            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(new { error = res.ServerError.ToString() }),
                "application/json");
       
        }

        [HttpGet()]
        [Authorize]
        public async Task<ActionResult> VZDetail(string _id)
        {
            string id = _id;

            if (string.IsNullOrEmpty(id))
                return new NotFoundResult();

            var client = await Manager.GetESClient_VerejneZakazkyNaProfiluRawAsync();
            var res = await client.SearchAsync<ZakazkaRaw>(s => s
                    .Query(q => q
                        .QueryString(qs => qs.DefaultOperator(Operator.And).Query("zakazkaId:\"" + id + "\""))
                    )
                    .Size(50)
                );

            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                res.Hits.Select(m => m.Source).FirstOrDefault()
            ), "application/json");
        }

        static string ReadRequestBody(HttpRequest req)
        {
            string ret = "";
            using (var stream = new MemoryStream())
            {
                req.Body.Seek(0, SeekOrigin.Begin);
                req.Body.CopyTo(stream);
                ret = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }

            return ret;
        }

        public async Task<ActionResult> Status()
        {
            ClusterHealthResponse res = null;
            int num = 0;
            string status = "unknown";
            string nodes = "-------------------------\n";
            try
            {
                var client = await Manager.GetESClientAsync(); 
                res = client.Cluster.Health();
                num = res?.NumberOfNodes ?? 0;
                status = res?.Status.ToString() ?? "unknown";

                //GET /_cat/nodes?v&h=m,name,ip,u&s=name
                var catr = client.LowLevel.Cat.Nodes<Elasticsearch.Net.StringResponse>(
                        new Elasticsearch.Net.Specification.CatApi.CatNodesRequestParameters()
                        {
                            Headers = new[] { "m", "name", "ip", "u" },
                            SortByColumns = new[] { "name" },
                            Verbose = true
                        }
                    ).Body
                    ?.Replace("10.10.", "");

                nodes = nodes + catr;
            }
            catch (Exception e)
            {
                Util.Consts.Logger.Error("Status page error", e);
                ViewBag.Error = e;
            }

            return Content(string.Format("{0}-{1}\n\n" + nodes, num, status), "text/plain");
        }

        public ActionResult Persons(string q)
        {
            ApiV1Models.PolitikTypeAhead[] res = new ApiV1Models.PolitikTypeAhead[] { };

            if (string.IsNullOrEmpty(q))
                return Json(res);

            if (q.Length < 2)
                return Json(res);

            res = OsobaRepo.Searching.GetPolitikByNameFtx(q, 15)
                .Select(m => new ApiV1Models.PolitikTypeAhead() { name = m.FullNameWithYear(), nameId = m.NameId })
                .ToArray();

            if (!string.IsNullOrEmpty(Request.Headers["Origin"]))
            {
                if (Request.Headers["Origin"].Contains(".hlidacstatu.cz")
                //|| Request.Headers["Origin"].Contains(".hlidacstatu.cz")
                )
                    Response.Headers.Add("Access-Control-Allow-Origin", Request.Headers["Origin"]);
            }
            return Json(res);
        }

        [Authorize(Roles = "NasiPoliticiAdmin")]
        public ActionResult AddInfo(string q, int? t)
        {
            if (!t.HasValue)
                return Json("");
            List<string> result = new();

            //if (string.IsNullOrEmpty(q) || q.Length <2)
            //    return Json(result);

            result = OsobaEventRepo.GetAddInfos(q, t, 200).ToList();


            if (!string.IsNullOrEmpty(Request.Headers["Origin"]))
            {
                if (Request.Headers["Origin"].Contains(".hlidacstatu.cz")
                //|| Request.Headers["Origin"].Contains(".hlidacstatu.cz")
                )
                    Response.Headers.Add("Access-Control-Allow-Origin", Request.Headers["Origin"]);
            }

            return Json(result);
        }

        [Authorize(Roles = "NasiPoliticiAdmin")]
        public ActionResult Organisations(string q, int? t)
        {
            if (!t.HasValue)
                return Json("");
            List<string> result = new();

            //if (string.IsNullOrEmpty(q) || q.Length <2)
            //    return Json(result);

            result = OsobaEventRepo.GetOrganisations(q, t, 200).ToList();


            if (!string.IsNullOrEmpty(Request.Headers["Origin"]))
            {
                if (Request.Headers["Origin"].Contains(".hlidacstatu.cz")
                //|| Request.Headers["Origin"].Contains(".hlidacstatu.cz")
                )
                    Response.Headers.Add("Access-Control-Allow-Origin", Request.Headers["Origin"]);
            }

            return Json(result);
        }

        [Authorize(Roles = "NasiPoliticiAdmin")]
        public async Task<IActionResult> Companies(
            [FromServices] IHttpClientFactory _httpClientFactory,
            string q,
            CancellationToken ctx)
        {
            var autocompleteHost = Devmasters.Config.GetWebConfigValue("AutocompleteEndpoint");
            var autocompletePath = $"/autocomplete/Companies?q={q}";
            var uri = new Uri($"{autocompleteHost}{autocompletePath}");
            using var client = _httpClientFactory.CreateClient(Constants.DefaultHttpClient);

            try
            {
                var response = await client.GetAsync(uri, ctx);
                if (!string.IsNullOrEmpty(Request.Headers["Origin"]))
                {
                    if (Request.Headers["Origin"].Contains(".hlidacstatu.cz"))
                        Response.Headers.Add("Access-Control-Allow-Origin", Request.Headers["Origin"]);
                }
                return new HttpResponseMessageActionResult(response);
            }
            catch (Exception ex) when ( ex is OperationCanceledException || ex is TaskCanceledException)
            {
                // canceled by user
                Util.Consts.Logger.Info("Autocomplete canceled by user");
            }
            catch (Exception e)
            {
                Util.Consts.Logger.Warning("Autocomplete API problem.", e, new { q });
            }
            
            return NoContent();
        }

        public async Task<IActionResult> Adresy(
            [FromServices] IHttpClientFactory _httpClientFactory,
            string q,
            CancellationToken ctx)
        {
            var autocompleteHost = Devmasters.Config.GetWebConfigValue("AutocompleteEndpoint");
            var autocompletePath = $"/autocomplete/Adresy?q={q}";
            var uri = new Uri($"{autocompleteHost}{autocompletePath}");
            using var client = _httpClientFactory.CreateClient(Constants.DefaultHttpClient);

            try
            {
                var response = await client.GetAsync(uri, ctx);
                if (!string.IsNullOrEmpty(Request.Headers["Origin"]))
                {
                    if (Request.Headers["Origin"].Contains(".hlidacstatu.cz"))
                        Response.Headers.Add("Access-Control-Allow-Origin", Request.Headers["Origin"]);
                }
                return new HttpResponseMessageActionResult(response);
            }
            catch (Exception ex) when ( ex is OperationCanceledException || ex is TaskCanceledException)
            {
                // canceled by user
                Util.Consts.Logger.Info("Autocomplete canceled by user");
            }
            catch (Exception e)
            {
                Util.Consts.Logger.Warning("Autocomplete API problem.", e, new { q });
            }
            
            return NoContent();
        }
        


        [Authorize]
        public async Task<ActionResult> Search(string query, int? page, int? order)
        {
            page = page ?? 1;
            order = order ?? 0;
            Repositories.Searching.SmlouvaSearchResult res = null;
            
            if (string.IsNullOrWhiteSpace(query))
                return new NotFoundResult();

            bool? platnyzaznam = null; //1 - nic defaultne
            if (
                System.Text.RegularExpressions.Regex.IsMatch(query.ToLower(), "(^|\\s)id:")
                ||
                query.ToLower().Contains("idverze:")
                ||
                query.ToLower().Contains("idsmlouvy:")
                ||
                query.ToLower().Contains("platnyzaznam:")
            )
                platnyzaznam = null;

            res = await SmlouvaRepo.Searching.SimpleSearchAsync(query, page.Value,
                SmlouvaRepo.Searching.DefaultPageSize,
                (SmlouvaRepo.Searching.OrderResult)order.Value,
                platnyZaznam: platnyzaznam);

            if (res.IsValid == false)
            {
                Response.StatusCode = 500;
                return Json(new { error = "Bad query", reason = res.ElasticResults.ServerError });
            }
            else
            {
                var filtered = res.Results
                    .Select(m =>
                        Smlouva.Export(m, HttpContext.User.IsInRole("Admin"), false))
                    .ToArray();

                return Content(
                    Newtonsoft.Json.JsonConvert.SerializeObject(new { total = res.Total, items = filtered },
                        Newtonsoft.Json.Formatting.None), "application/json");
            }
        }

        [Authorize]
        public async Task<ActionResult> Detail(string _id)
        {
            string Id = _id;
           
            if (string.IsNullOrWhiteSpace(Id))
                return new NotFoundResult();

            var model = await SmlouvaRepo.LoadAsync(Id);
            if (model == null)
            {
                return new NotFoundResult();
            }

            var smodel = Smlouva.Export(model,
                allData: HttpContext.User.IsInRole("Admin"),
                docsContent: true
            );
            var s = Newtonsoft.Json.JsonConvert.SerializeObject(
                smodel,
                new Newtonsoft.Json.JsonSerializerSettings()
                {
                    Formatting = (Request.Query["nice"] == "1"
                        ? Newtonsoft.Json.Formatting.Indented
                        : Newtonsoft.Json.Formatting.None),
                    //NullValueHandling = NullValueHandling.Ignore,         
                    ContractResolver = new Util.FirstCaseLowercaseContractResolver()
                }
            );

            return Content(s, "application/json");
        }

        [Authorize]
        public async Task<ActionResult> ClassificationList(int pageSize = 200)
        {
            string urlItemTemplate = "https://www.hlidacstatu.cz/api/v1/GetForClassification/{0}";

            if (pageSize > 500)
                pageSize = 500;

            var items = await SmlouvaRepo.Searching.SimpleSearchAsync("NOT(_exists_:prilohy.datlClassification)", 0, pageSize,
                SmlouvaRepo.Searching.OrderResult.DateAddedDesc, platnyZaznam: true);

            if (!items.IsValid)
            {
                Response.StatusCode = 400;
                return Json(null);
            }

            var result = new ApiV1Models.ClassificatioListItemModel();

            var contracts = items.ElasticResults.Hits
                .Select(m => new ApiV1Models.ClassificatioListItemModel.Contract()
                {
                    contractId = m.Source.Id,
                    url = string.Format(urlItemTemplate, m.Source.Id)
                }
                )
                .ToArray();
            result.contracts = contracts;

            return Json(result);
        }

        [Obsolete]
        [Authorize]
        public async Task<ActionResult> GetForClassification(string _id)
        {
            string Id = _id;

            if (string.IsNullOrWhiteSpace(Id))
                return new NotFoundResult();

            var model = await SmlouvaRepo.LoadAsync(Id);
            if (model == null)
            {
                return new NotFoundResult();
            }

            if (model.znepristupnenaSmlouva() && model.Prilohy != null)
            {
                foreach (var p in model.Prilohy)
                {
                    p.PlainTextContent = "-- anonymizovano serverem hlidacstatu.cz --";
                    p.odkaz = "";
                }
            }

            if (model.Prilohy != null)
            {
                await SmlouvaRepo.SaveAsync(model);
            }


            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(model, Newtonsoft.Json.Formatting.None),
                "application/json");
        
        }

        class osobaResult
        {
            public osobaResult(Osoba o)
            {
                TitulPred = o.TitulPred;
                Jmeno = o.Jmeno;
                Prijmeni = o.Prijmeni;
                TitulPo = o.TitulPo;
                Narozeni = o.Narozeni;
                NameId = o.NameId;
                Profile = o.GetUrl();
            }

            public string TitulPred { get; set; }
            public string Jmeno { get; set; }
            public string Prijmeni { get; set; }
            public string TitulPo { get; set; }

            public DateTime? Narozeni { get; set; }
            public string NameId { get; set; }
            public string Profile { get; set; }
        }

        [Authorize(Roles = "TeamMember")]
        public ActionResult OsobaPridat(string jmeno, string prijmeni, string narozeni, string titulPred,
            string titulPo, int typOsoby)
        {
            DateTime? nar = Devmasters.DT.Util.ToDateTimeFromCode(narozeni);
            if (nar.HasValue == false)
            {
                return Content(
                    Newtonsoft.Json.JsonConvert.SerializeObject(new
                    { valid = false, error = "Invalid date format. Use yyyy-MM-dd format." }),
                    "application/json");
            }

            if (typOsoby < 0 || typOsoby > 3)
            {
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    valid = false,
                    error =
                        "Invalid typOsoby. Use 0 = NeniPolitik , 1 = ByvalyPolitik , 2 = VazbyNaPolitiky , 3 = Politik."
                }));
            }

            var no = OsobaRepo.GetOrCreateNew(titulPred, jmeno, prijmeni, titulPo, nar,
                (Osoba.StatusOsobyEnum)typOsoby, HttpContext.User.Identity.Name);
            no.Vazby(true);

            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                    new { valid = true, nameId = no.NameId })
                , "application/json");
        }

        [Authorize(Roles = "TeamMember")]
        public async Task<ActionResult> PolitikFromText(string text)
        {
            var oo = await OsobaRepo.Searching.GetFirstPolitikFromTextAsync(text);

            if (oo != null)
            {
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                    new { osobaid = oo.NameId, jmeno = oo.Jmeno, prijmeni = oo.Prijmeni }
                ), "application/json");
            }
            else
            {
                return Content("{}", "application/json");
            }
        }

        [Authorize(Roles = "TeamMember")]
        public async Task<ActionResult> PoliticiFromText(string text)
        {
            var oo = await OsobaRepo.Searching.GetBestPoliticiFromTextAsync(text);

            if (oo != null)
            {
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                    oo.Select(o => new { osobaid = o.NameId, jmeno = o.Jmeno, prijmeni = o.Prijmeni })
                        .ToArray()
                ), "application/json");
            }
            else
            {
                return Content("[]", "application/json");
            }
        }

        [Authorize(Roles = "TeamMember")]
        public ActionResult OsobaHledat(string jmeno, string prijmeni, string narozen)
        {
            DateTime? dt = Devmasters.DT.Util.ToDateTime(narozen
                , "yyyy-MM-dd");
            if (dt.HasValue == false)
            {
                return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                    new { error = "invalid date format. Use yyyy-MM-dd format." }
                ), "application/json");
            }

            var found = OsobaRepo.Searching.GetAllByNameAscii(jmeno, prijmeni, dt.Value)
                .Select(o => new osobaResult(o))
                .ToArray();

            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                new { Total = found.Count(), Result = found }
            ), "application/json");
        }

        public async Task<ActionResult> CheckText(string smlouvaid)
        {
            Entities.Issues.IIssueAnalyzer textCheck = new Plugin.IssueAnalyzers.Text();
            Smlouva s = await SmlouvaRepo.LoadAsync(smlouvaid);
            if (s != null)
            {
                if (s.Prilohy != null && s.Prilohy.Count() > 0)
                {
                    var newIss = s.Issues.Where(m => m.IssueTypeId != 200).ToList();
                    newIss.AddRange(await textCheck.FindIssuesAsync(s));
                    s.Issues = newIss.ToArray();
                    await SmlouvaRepo.SaveAsync(s, fireOCRDone: false);
                }
            }

            return Content("OK");
        }
    }
}