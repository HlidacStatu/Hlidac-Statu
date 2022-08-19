using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Devmasters;
using Devmasters.Enums;
using Devmasters.Net.HttpClient;

using HlidacStatu.Connectors;
using HlidacStatu.Datasets;
using HlidacStatu.Datastructures.Graphs;
using HlidacStatu.Entities;
using HlidacStatu.Entities.VZ;
using HlidacStatu.Extensions;
using HlidacStatu.LibCore.Extensions;
using HlidacStatu.Repositories;
using HlidacStatu.Repositories.ES;
using HlidacStatu.Web.Filters;
using HlidacStatu.Web.Framework;

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

using Visit = HlidacStatu.Web.Framework.Visit;


namespace HlidacStatu.Web.Controllers
{
    public partial class HomeController : Controller
    {

        private readonly UserManager<ApplicationUser> _userManager;
        protected readonly IWebHostEnvironment _hostingEnvironment;
        private readonly TelemetryClient _telemetryClient;

        public HomeController(IWebHostEnvironment hostingEnvironment, UserManager<ApplicationUser> userManager, TelemetryClient telemetryClient)
        {
            _hostingEnvironment = hostingEnvironment;
            _userManager = userManager;
            _telemetryClient = telemetryClient;
        }

        [Authorize]
        public async Task<ActionResult> Kod(string? id)
        {

            if (id != null && id?.ToLower()?.RemoveAccents()?.Equals("lizatko") == true)
            {
                var user = await _userManager.GetUserAsync(HttpContext.User);
                await _userManager.AddToRoleAsync(user, "TK-KIndex-2021");

                return View(true);
            }

            return View(false);


        }

        public ActionResult Analyza(string p, string q, string title, string description, string moreUrl, int? y)
        {
            ViewData.Add(Constants.CacheKeyName,
                WebUtil.GenerateCacheKey(new object[] { p, q, title, description, moreUrl, y }));

            var model = new Lib.Analysis.TemplatedQuery() { Query = q, Text = title, Description = description };


            if (StaticData.Afery.ContainsKey(p?.ToLower() ?? ""))
                model = StaticData.Afery[p.ToLower()];
            else if (!string.IsNullOrEmpty(q))
            {
                model = new Lib.Analysis.TemplatedQuery() { Query = q, Text = title, Description = description };
                if (Uri.TryCreate(moreUrl, UriKind.Absolute, out var uri))
                    model.Links = new Lib.Analysis.TemplatedQuery.AHref[] {
                        new(uri.AbsoluteUri,"více informací")
                    };
            }

            return View(model);
        }

        public ActionResult Photo(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                string f = HttpContext.Request.Query["f"];
                if (string.IsNullOrEmpty(f) || f?.Contains("..") == true)
                    return File(System.IO.File.ReadAllBytes(Init.WebAppRoot + @"Content\Img\personNoPhoto.png"), "image/png");

                var nameId = Devmasters.RegexUtil.GetRegexGroupValue(f, @"\d{2} \\ (?<nameid>\w* - \w* (-\d{1,3})?) - small\.jpg", "nameid");
                if (string.IsNullOrEmpty(nameId))
                    return File(System.IO.File.ReadAllBytes(Init.WebAppRoot + @"Content\Img\personNoPhoto.png"), "image/png");
                else
                    return Redirect("/photo/" + nameId);
            }
            var o = Osoby.GetByNameId.Get(id);
            if (o == null)
                return File(System.IO.File.ReadAllBytes(Init.WebAppRoot + @"Content\Img\personNoPhoto.png"), "image/png");
            else
            {
                if (o.HasPhoto())
                    return File(System.IO.File.ReadAllBytes(o.GetPhotoPath()), "image/jpg");
                else
                    return File(System.IO.File.ReadAllBytes(Init.WebAppRoot + @"Content\Img\personNoPhoto.png"), "image/png");
            }
        }


        public ActionResult Reporty()
        {
            return View();
        }

        public ActionResult ProvozniPodminky()
        {
            return RedirectPermanent("/texty/provoznipodminky");
        }

        public ActionResult Index()
        {
            return View();
        }

        [ActionName("K-Index")]
        public ActionResult Kindex()
        {
            return RedirectPermanent("/kindex");
        }


        public ActionResult PridatSe()
        {
            return RedirectPermanent("https://www.hlidacstatu.cz/texty/pridejte-se/");
        }


        public ActionResult VerejneZakazky(string q)
        {
            return Redirect("/VerejneZakazky");
        }

        public ActionResult Licence()
        {
            return RedirectPermanent("/texty/licence");
        }

        public ActionResult OServeru()
        {

            return RedirectPermanent("/texty/o-serveru");
        }

        public ActionResult Zatmivaci()
        {

            return View();
        }
        public ActionResult VisitImg(string id)
        {
            try
            {
                var path = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(id));
                Visit.AddVisit(path,
                    Visit.IsCrawler(Request.Headers[HeaderNames.UserAgent]) ?
                        Visit.VisitChannel.Crawler : Visit.VisitChannel.Web);
            }
            catch (Exception e)
            {
                Util.Consts.Logger.Info("VisitImg base64 encoding error", e);
            }

            return File(@"Content\Img\1x1.png", "image/png");
        }

        public ActionResult Kontakt()
        {
            return RedirectPermanent("/texty/kontakt");
        }


        public async Task<ActionResult> sendFeedbackMail(string typ, string email, string txt, string url, bool? auth, string data)
        {
            string to = "podpora@hlidacstatu.cz";
            string subject = "Zprava z HlidacStatu.cz: " + typ;
            if (!string.IsNullOrEmpty(data))
            {
                if (data.StartsWith("dataset|"))
                {
                    data = data.Replace("dataset|", "");
                    try
                    {
                        var ds = DataSet.CachedDatasets.Get(data);
                        to = (await ds.RegistrationAsync()).createdBy;
                        subject = subject + $" ohledně databáze {ds.DatasetId}";
                    }
                    catch (Exception)
                    {
                        return Content("");
                    }
                }
            }

            if (auth == false || (auth == true && User?.Identity?.IsAuthenticated == true))
            {
                if (!string.IsNullOrEmpty(email) && Devmasters.TextUtil.IsValidEmail(email)
                    && !string.IsNullOrEmpty(to) && Devmasters.TextUtil.IsValidEmail(to)
                    )
                {
                    try
                    {
                        string body = $@"
Zpráva z hlidacstatu.cz.

Typ zpravy:{typ}
Od uzivatele:{email} 
ke stránce:{url}

text zpravy: {txt}";
                        Util.SMTPTools.SendSimpleMailToPodpora(subject, body, email);

                    }
                    catch (Exception ex)
                    {

                        Util.Consts.Logger.Fatal(string.Format("{0}|{1}|{2}", email, url, txt, ex));
                        return Content("");
                    }
                }
            }
            return Content("");
        }

        public ActionResult ClassificationFeedback(string typ, string email, string txt, string url, string data)
        {
            // create a task, so user doesn't have to wait for anything
            _ = Task.Run(async () =>
            {

                try
                {
                    string subject = "Zprava z HlidacStatu.cz: " + typ;
                    string body = $@"
Návrh na opravu klasifikace.

Od uzivatele:{email} 
ke stránce:{url}

text zpravy: {txt}

";
                    Util.SMTPTools.SendSimpleMailToPodpora(subject, body, email);

                    string classificationExplanation = await SmlouvaRepo.GetClassificationExplanationAsync(data);

                    string explain = $"explain result: {classificationExplanation} ";

                    Util.SMTPTools.SendEmail(subject, "", body + explain, "michal@michalblaha.cz");
                    Util.SMTPTools.SendEmail(subject, "", body + explain, "petr@hlidacstatu.cz");
                    Util.SMTPTools.SendEmail(subject, "", body + explain, "lenka@hlidacstatu.cz");
                }
                catch (Exception ex)
                {
                    Util.Consts.Logger.Fatal(string.Format("{0}|{1}|{2}", email, url, txt, ex));
                }

                try
                {
                    string connectionString = Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString");
                    if (string.IsNullOrWhiteSpace(connectionString))
                        throw new Exception("Missing RabbitMqConnectionString");

                    var message = new Q.Messages.ClassificationFeedback()
                    {
                        FeedbackEmail = email,
                        IdSmlouvy = data,
                        ProposedCategories = txt
                    };

                    Q.Publisher.QuickPublisher.Publish(message, connectionString);
                }
                catch (Exception ex)
                {
                    Util.Consts.Logger.Fatal($"Problem sending data to ClassificationFeedback queue. Message={ex}");
                }


            });

            return Content("");
        }

        public async Task<ActionResult> TextSmlouvy(string Id, string hash, string secret)
        {
            if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(hash))
                return NotFound();

            var model = await SmlouvaRepo.LoadAsync(Id);
            if (model == null)
            {
                return NotFound();
            }
            var priloha = model.Prilohy?.FirstOrDefault(m => m.hash.Value == hash);
            if (priloha == null)
            {
                return NotFound();
            }

            if (model.znepristupnenaSmlouva())
            {
                if (string.IsNullOrEmpty(secret)) //pokus jak se dostat k znepristupnene priloze
                    return Redirect(model.GetUrl(true)); //jdi na detail smlouvy
                else if (User?.Identity?.IsAuthenticated == false) //neni zalogovany
                    return Redirect(model.GetUrl(true)); //jdi na detail smlouvy
                else
                {
                    if (priloha.LimitedAccessSecret(User.Identity.Name) != secret)
                        return Redirect(model.GetUrl(true)); //jdi na detail smlouvy
                }
            }

            ViewBag.hashValue = hash;
            return View(model);
        }
        public async Task<ActionResult> KopiePrilohy(string Id, string hash, string secret)
        {
            if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(hash))
                return NotFound();

            var model = await SmlouvaRepo.LoadAsync(Id);
            if (model == null)
            {
                return NotFound();
            }

            var priloha = model.Prilohy?.FirstOrDefault(m => m.UniqueHash() == hash);
            if (priloha == null)
            {
                return NotFound();
            }

            if (model.znepristupnenaSmlouva())
            {
                if (User.IsInRole("Admin") == false && secret != Devmasters.Config.GetWebConfigValue("LocalPrilohaUniversalSecret"))
                {
                    if (string.IsNullOrEmpty(secret)) //pokus jak se dostat k znepristupnene priloze
                        return Redirect(model.GetUrl(true)); //jdi na detail smlouvy
                    else if (User?.Identity?.IsAuthenticated == false) //neni zalogovany
                        return Redirect(model.GetUrl(true)); //jdi na detail smlouvy
                    else if (User?.HasEmailConfirmed() == false)
                    {
                        return Redirect(model.GetUrl(true)); //jdi na detail smlouvy
                    }
                    else
                    {
                        if (priloha.LimitedAccessSecret(User?.Identity?.Name) != secret)
                            return Redirect(model.GetUrl(true)); //jdi na detail smlouvy
                    }
                }
            }
            var fn = Init.PrilohaLocalCopy.GetFullPath(model, priloha);
            if (System.IO.File.Exists(fn) == false)
                return NotFound();


            //if (System.IO.File.Exists(fn) == false)
            //{
            //    if (Uri.TryCreate(priloha.odkaz, UriKind.Absolute, out _) == false)
            //        return NotFound();

            //    //download from registr smluv
            //    try
            //    {
            //        using (URLContent url = new URLContent(priloha.odkaz))
            //        {
            //            url.Timeout = url.Timeout * 10;
            //            byte[] data = url.GetBinary().Binary;
            //            System.IO.File.WriteAllBytes(fn, data);
            //            //p.LocalCopy = System.Text.UTF8Encoding.UTF8.GetBytes(io.GetRelativePath(item, p));
            //        }
            //    }
            //    catch (Exception e)
            //    {
            //        Util.Consts.Logger.Error(priloha.odkaz, e);
            //        return NotFound();
            //    }

            //}
            if (Lib.OCR.DocTools.HasPDFHeader(fn))
            {
                return File(System.IO.File.ReadAllBytes(fn), "application/pdf", string.IsNullOrWhiteSpace(priloha.nazevSouboru) ? $"{model.Id}_smlouva.pdf" : priloha.nazevSouboru);
            }
            else
                return File(System.IO.File.ReadAllBytes(fn),
                    string.IsNullOrWhiteSpace(priloha.ContentType) ? "application/octet-stream" : priloha.ContentType,
                    string.IsNullOrWhiteSpace(priloha.nazevSouboru) ? "priloha" : priloha.nazevSouboru);

        }

        public ActionResult PoliticiChybejici()
        {

            return View();
        }

        public ActionResult Api(string id)
        {
            return RedirectToActionPermanent("Index", "ApiV1");
        }

        public ActionResult JsonDoc()
        {
            return View();
        }
        public ActionResult Smlouvy()
        {
            return View();
        }
        public ActionResult ORegistru()
        {
            return RedirectPermanent("https://www.hlidacstatu.cz/texty/o-registru");
        }

        public ActionResult Afery()
        {
            return View();
        }


        public ActionResult HledatVice()
        {
            return RedirectToActionPermanent("SnadneHledani");
        }
        public ActionResult SnadneHledani()
        {
            string[] splitChars = new string[] { " " };
            var qs = HttpContext.Request.Query;

            string query = "";


            if (!string.IsNullOrWhiteSpace(qs["alltxt"]))
            {
                query += " " + qs["alltxt"];
            }
            if (!string.IsNullOrWhiteSpace(qs["exacttxt"]))
            {
                query += " \"" + qs["exacttxt"] + "\"";
            }
            if (!string.IsNullOrWhiteSpace(qs["anytxt"]))
            {
                query += " ("
                    + qs["anytxt"].ToString().Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Aggregate((f, s) => f + " OR " + s)
                    + ")";
            }
            if (!string.IsNullOrWhiteSpace(qs["nonetxt"]))
            {
                query += " " + qs["nonetxt"].ToString().Split(splitChars, StringSplitOptions.RemoveEmptyEntries).Select(s => s.StartsWith("-") ? s : "-" + s).Aggregate((f, s) => f + " " + s);
            }
            if (!string.IsNullOrWhiteSpace(qs["textsmlouvy"]))
            {
                query += " textSmlouvy:\"" + qs["textsmlouvy"].ToString().Trim() + "\"";
            }


            List<KeyValuePair<string, string>> platce = new();
            if (qs["icoPlatce"].ToString() != null)
                foreach (var val in qs["icoPlatce"]
                    .ToString().Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    )
                { platce.Add(new KeyValuePair<string, string>("icoPlatce", val)); }


            if (qs["dsPlatce"].ToString() != null)
                foreach (var val in qs["dsPlatce"]
                        .ToString().Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    )
                { platce.Add(new KeyValuePair<string, string>("dsPlatce", val)); }


            platce.Add(new KeyValuePair<string, string>("jmenoPlatce", qs["jmenoPlatce"]));
            if (platce.Count(m => !string.IsNullOrWhiteSpace(m.Value)) > 1)
            { // into ()
                query += " ("
                        + platce.Where(m => !string.IsNullOrWhiteSpace(m.Value)).Select(m => m.Key + ":" + m.Value).Aggregate((f, s) => f + " OR " + s)
                        + ")";
            }
            else if (platce.Count(m => !string.IsNullOrWhiteSpace(m.Value)) == 1)
            {
                query += " " + platce.Where(m => !string.IsNullOrWhiteSpace(m.Value)).Select(m => m.Key + ":" + m.Value).First();
            }


            List<KeyValuePair<string, string>> prijemce = new();
            if (qs["icoPrijemce"].ToString() != null)
                foreach (var val in qs["icoPrijemce"]
                    .ToString().Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    )
                { prijemce.Add(new KeyValuePair<string, string>("icoPrijemce", val)); }


            if (qs["dsPrijemce"].ToString() != null)
                foreach (var val in qs["dsPrijemce"]
                        .ToString().Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    )
                { prijemce.Add(new KeyValuePair<string, string>("dsPrijemce", val)); }



            prijemce.Add(new KeyValuePair<string, string>("jmenoprijemce", qs["jmenoprijemce"]));
            if (prijemce.Count(m => !string.IsNullOrWhiteSpace(m.Value)) > 1)
            { // into ()
                query += " ("
                        + prijemce.Where(m => !string.IsNullOrWhiteSpace(m.Value)).Select(m => m.Key + ":" + m.Value).Aggregate((f, s) => f + " OR " + s)
                        + ")";
            }
            else if (prijemce.Count(m => !string.IsNullOrWhiteSpace(m.Value)) == 1)
            {
                query += " " + prijemce.Where(m => !string.IsNullOrWhiteSpace(m.Value)).Select(m => m.Key + ":" + m.Value).First();
            }


            if (!string.IsNullOrWhiteSpace(qs["cenaOd"]) && Devmasters.TextUtil.IsNumeric(qs["cenaOd"]))
                query += " cena:>" + qs["cenaOd"];

            if (!string.IsNullOrWhiteSpace(qs["cenaDo"]) && Devmasters.TextUtil.IsNumeric(qs["cenaDo"]))
                query += " cena:<" + qs["cenaDo"];

            if (!string.IsNullOrWhiteSpace(qs["zverejnenoOd"]) && !string.IsNullOrWhiteSpace(qs["zverejnenoDo"]))
            {
                query += $" zverejneno:[{qs["zverejnenoOd"]} TO {qs["zverejnenoDo"]}]";
            }
            else if (!string.IsNullOrWhiteSpace(qs["zverejnenoOd"]))
            {
                query += $" zverejneno:[{qs["zverejnenoOd"]} TO *]";
            }
            else if (!string.IsNullOrWhiteSpace(qs["zverejnenoDo"]))
            {
                query += $" zverejneno:[* TO {qs["zverejnenoDo"]}]";
            }

            if (!string.IsNullOrWhiteSpace(qs["podepsanoOd"]) && !string.IsNullOrWhiteSpace(qs["podepsanoDo"]))
            {
                query += $" podepsano:[{qs["podepsanoOd"]} TO {qs["podepsanoDo"]}]";
            }
            else if (!string.IsNullOrWhiteSpace(qs["podepsanoOd"]))
            {
                query += $" podepsano:[{qs["podepsanoOd"]} TO *]";
            }
            else if (!string.IsNullOrWhiteSpace(qs["podepsanoDo"]))
            {
                query += $" podepsano:[* TO {qs["podepsanoDo"]}]";
            }


            if (!string.IsNullOrWhiteSpace(qs["osobaNamedId"]))
            {
                query += " osobaid:" + qs["osobaNamedId"];
            }
            if (!string.IsNullOrWhiteSpace(qs["holding"]))
            {
                query += " holding:" + qs["holding"];
            }

            if (!string.IsNullOrWhiteSpace(qs["obory"]))
            {
                foreach (var obor in qs["obory"])
                {
                    query += " oblast:" + obor;
                }
            }

            query = query.Trim();

            if (!string.IsNullOrWhiteSpace(query))
            {
                if (!string.IsNullOrEmpty(qs["hledatvse"]))
                    return Redirect("/Hledat?q=" + System.Net.WebUtility.UrlEncode(query));

                if (!string.IsNullOrEmpty(qs["hledatsmlouvy"]))
                    return Redirect("/HledatSmlouvy?q=" + System.Net.WebUtility.UrlEncode(query));

                if (!string.IsNullOrEmpty(qs["hledatvz"]))
                    return Redirect("/VerejneZakazky/Hledat?q=" + System.Net.WebUtility.UrlEncode(query));
            }

            return View();
        }

        public ActionResult Adresar(string id, string kraj = null)
        {
            (string oborName, string kraj) model = (id, kraj);
            return View(model);
        }

        public ActionResult Politici(string prefix)
        {
            return RedirectPermanent(Url.Action("Index", "Osoby", new { prefix = prefix }));
        }

        public ActionResult Politik(string Id, Relation.AktualnostType? aktualnost)
        {
            return RedirectPermanent(Url.Action("Index", "Osoba", new { Id = Id, aktualnost = aktualnost }));
        }

        public ActionResult PolitikVazby(string Id, Relation.AktualnostType? aktualnost)
        {
            return RedirectPermanent(Url.Action("Vazby", "Osoba", new { Id = Id, aktualnost = aktualnost }));
        }

        public async Task<ActionResult> Detail(string Id)
        {
            if (string.IsNullOrWhiteSpace(Id))
                return NotFound();

            var model = await SmlouvaRepo.LoadAsync(Id);
            if (model == null)
            {
                return NotFound();
            }
            if (!string.IsNullOrEmpty(HttpContext.Request.Query["qs"]))
            {
                var findSm = await SmlouvaRepo.Searching
                    .SimpleSearchAsync($"_id:\"{model.Id}\" AND ({HttpContext.Request.Query["qs"]})", 1, 1,
                    SmlouvaRepo.Searching.OrderResult.FastestForScroll, withHighlighting: true);
                if (findSm.Total > 0)
                    ViewBag.Highlighting = findSm.ElasticResults.Hits.First().Highlight;

            }
            return View(model);
        }

        public async Task<ActionResult> HledatFirmy(string q, int? page = 1)
        {
            var model = await FirmaRepo.Searching.SimpleSearchAsync(q, page.Value, 50);
            return View(model);
        }

        public async Task<ActionResult> HledatSmlouvy(Repositories.Searching.SmlouvaSearchResult model)
        {
            if (model == null || ModelState.IsValid == false)
                return View(new Repositories.Searching.SmlouvaSearchResult());



            var sres = await SmlouvaRepo.Searching.SimpleSearchAsync(model.Q, model.Page,
                SmlouvaRepo.Searching.DefaultPageSize,
                (SmlouvaRepo.Searching.OrderResult)(Convert.ToInt32(model.Order)),
                includeNeplatne: model.IncludeNeplatne,
                anyAggregation: new Nest.AggregationContainerDescriptor<Smlouva>().Sum("sumKc", m => m.Field(f => f.CalculatedPriceWithVATinCZK)),
                logError: false);

            AuditRepo.Add(
                    Audit.Operations.UserSearch
                    , User?.Identity?.Name
                    , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                    , "Smlouva"
                    , sres.IsValid ? "valid" : "invalid"
                    , sres.Q, sres.OrigQuery);

            if (sres.IsValid == false && !string.IsNullOrEmpty(sres.Q))
            {
                Manager.LogQueryError<Smlouva>(sres.ElasticResults, "/hledat", HttpContext);
            }

            return View(sres);
        }

        public async Task<ActionResult> Hledat(string q, string order)
        {
            bool showBeta = User.Identity?.IsAuthenticated == true && User.IsInRole("BetaTester");

            var res = await XLib.Search
                .GeneralSearchAsync(q, 1, Repositories.Searching.SearchDataResult<object>.DefaultPageSizeGlobal, showBeta, order);
            AuditRepo.Add(
                Audit.Operations.UserSearch
                , User?.Identity?.Name
                , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                , "General"
                , res.IsValid ? "valid" : "invalid"
                , q, null);

            if (System.Diagnostics.Debugger.IsAttached ||
                Devmasters.Config.GetWebConfigValue("LogSearchTimes") == "true")
            {
                Util.Consts.Logger.Info($"Search times: {q}\n" + res.SearchTimesReport());

                var data = res.SearchTimes();


                // Set up some properties:

                //foreach (var kv in data)
                //{
                //    var metrics = new Dictionary<string, double> { { "web-search-" + kv.Key, kv.Value.TotalMilliseconds } };
                //    var props = new Dictionary<string, string> { { "query", q }, { "database", kv.Key } };

                //    Metric elaps = _telemetryClient.GetMetric("web-GlobalSearch_Elapsed", "Database");
                //    _telemetryClient.TrackEvent("web-GlobalSearch_Elapsed", props, metrics);
                //    var ok = elaps.TrackValue(kv.Value.TotalMilliseconds, kv.Key);
                //}
            }
            string viewName = "Hledat";
            return View(viewName, res);
        }

        public ActionResult Novinky()
        {
            return RedirectPermanent("https://www.hlidacstatu.cz/texty");
        }
        public ActionResult Napoveda()
        {

            return View();
        }
        public ActionResult Cenik()
        {

            return View();
        }

        public ActionResult PravniPomoc()
        {

            return View();
        }



        public ActionResult Error404(string nextUrl = null, string nextUrlText = null)
        {
            return NotFound();
        }


        public enum ErrorPages
        {
            Ok = 0,
            NotFound = 404,
            Error = 500,
            ErrorHack = 555
        }

        public ActionResult Error(string id, string nextUrl = null, string nextUrlText = null)
        {
            ViewBag.NextText = nextUrl;
            ViewBag.NextUrlText = nextUrlText;
            ViewBag.InvokeErrorAction = true;

            ErrorPages errp = (ErrorPages)EnumTools.GetValueOrDefaultValue(id, typeof(ErrorPages));

            switch (errp)
            {
                case ErrorPages.Ok:
                    return Redirect("/");
                case ErrorPages.NotFound:
                    return NotFound();
                case ErrorPages.Error:
                    return StatusCode((int)ErrorPages.Error);
                case ErrorPages.ErrorHack:
                    return StatusCode((int)ErrorPages.ErrorHack);
                default:
                    return Redirect("/");
            }
        }

        public ActionResult Widget(string id, string width)
        {
            string path = Path.Combine(_hostingEnvironment.WebRootPath, "Scripts\\widget.js");

            string widgetjs = System.IO.File.ReadAllText(path)
                .Replace("#RND#", id ?? Devmasters.TextUtil.GenRandomString(4))
                .Replace("#MAXWIDTH#", width != null ? width.ToString() : "")
                .Replace("#MAXWIDTHSCRIPT#", width != null ? ",maxWidth:" + width : "")
                .Replace("#WEBROOT#", HttpContext.Request.Scheme + "://" + HttpContext.Request.Host)
                ;
            return Content(widgetjs, "text/javascript");
        }

        [HlidacCache(60 * 60, "id", false)]
        public async Task<ActionResult> Export(string id)
        {
            System.Text.StringBuilder sb = new(2048);

            if (id == "uohs-ed")
            {
                var ds = DataSet.CachedDatasets.Get("rozhodnuti-uohs");
                var res = await ds.SearchDataAsync("*", 0, 30, "PravniMoc desc");
                if (res.Total > 0)
                {
                    sb.Append(
                        Newtonsoft.Json.JsonConvert.SerializeObject(
                            res.Result
                            .Select(m =>
                            {
                                m.DetailUrl = "https://www.hlidacstatu.cz/data/Detail/rozhodnuti-uohs/" + m.Id;
                                m.DbCreatedBy = null; m.Rozhodnuti = null;
                                return m;
                            })
                            )
                        );
                }
                else
                {
                    sb.Append("[]");
                }
            }
            else if (id == "vz-ed")
            {
                string[] icos = FirmaRepo.MinisterstvaCache.Get().Select(s => s.ICO).ToArray();

                var vz = VerejnaZakazkaRepo.Searching.CachedSimpleSearch(TimeSpan.FromHours(6),
                    new Repositories.Searching.VerejnaZakazkaSearchData()
                    {
                        Q = icos.Select(i => "ico:" + i).Aggregate((f, s) => f + " OR " + s),
                        Page = 0,
                        PageSize = 30,
                        Order = "1"
                    }
                    );
                if (vz.Total > 0)
                {
                    sb.Append(
                        Newtonsoft.Json.JsonConvert.SerializeObject(
                            vz.ElasticResults.Hits.Select(h => new
                            {
                                Id = h.Id,
                                DetailUrl = h.Source.GetUrl(false),
                                Zadavatel = h.Source.Zadavatel,
                                Dodavatele = h.Source.Dodavatele,
                                NazevZakazky = h.Source.NazevZakazky,
                                Cena = h.Source.FormattedCena(false),
                                CPVkody = h.Source.CPV.Count() == 0
                                        ? "" :
                                        h.Source.CPV.Select(c => h.Source.CPVText(c)).Aggregate((f, s) => f + ", " + s),
                                Stav = h.Source.StavZakazky.ToNiceDisplayName(),
                                DatumUverejneni = h.Source.DatumUverejneni
                            })
                            )
                        );
                }
                else
                {
                    sb.Append("[]");
                }
            }
            return Content(sb.ToString(), "application/json");
        }


        public class ImageBannerCoreData
        {
            public string title { get; set; }
            public string subtitle { get; set; }
            public string body { get; set; }
            public string footer { get; set; }
            public string img { get; set; }
            public string color { get; set; }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public ActionResult ImageBannerCore(string id, string title, string subtitle, string body, string footer, string img, string ratio = "16x9", string color = "blue-dark")
        {
            id = id ?? "social";

            string viewName = "ImageBannerCore16x9_social";
            if (id.ToLower() == "quote")
            {
                if (ratio == "1x1")
                    viewName = "ImageBannerCore1x1_quote";
                else
                    viewName = "ImageBannerCore16x9_quote";
            }
            else
            {
                if (ratio == "1x1")
                    viewName = "ImageBannerCore1x1_social";
                else
                    viewName = "ImageBannerCore16x9_social";
            }

            return View(viewName, new ImageBannerCoreData() { title = title, subtitle = subtitle, body = body, footer = footer, img = img, color = color });
        }

        public async Task<ActionResult> SocialBanner(string id, string v, string t, string st, string b, string f, string img, string rat = "16x9", string res = "1200x628", string col = "")
        {
            string mainUrl = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;


            // #if (DEBUG)
            //             if (System.Diagnostics.Debugger.IsAttached)
            //                 mainUrl = "http://local.hlidacstatu.cz";
            //             //mainUrl = "https://www.hlidacstatu.cz";
            // #endif
            //twitter Recommended size: 1024 x 512 pixels
            //fb Recommended size: 1200 pixels by 630 pixels

            string url = null;

            byte[] data = null;
            if (id?.ToLower() == "subjekt")
            {
                Firma fi = Firmy.Get(v);
                if (fi.Valid)
                {
                    if (!(await fi.NotInterestingToShowAsync()))
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = fi.SocialInfoTitle(),
                            body = fi.SocialInfoBody(),
                            footer = fi.SocialInfoFooter(),
                            subtitle = fi.SocialInfoSubTitle(),
                            img = fi.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }
            }
            else if (id?.ToLower() == "zakazka")
            {
                VerejnaZakazka vz = await VerejnaZakazkaRepo.LoadFromESAsync(v);
                if (vz != null)
                    try
                    {
                        var path = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(id));
                        Visit.AddVisit(path,
                            Visit.IsCrawler(Request.Headers[HeaderNames.UserAgent]) ?
                                Visit.VisitChannel.Crawler : Visit.VisitChannel.Web);
                    }
                    catch (Exception e)
                    {
                        Util.Consts.Logger.Info("VisitImg base64 encoding error", e);
                    }

                if (!vz.NotInterestingToShow())
                {
                    var social = new ImageBannerCoreData()
                    {
                        title = vz.SocialInfoTitle(),
                        body = vz.SocialInfoBody(),
                        footer = vz.SocialInfoFooter(),
                        subtitle = vz.SocialInfoSubTitle(),
                        img = vz.SocialInfoImageUrl(),
                        color = "blue-dark"
                    };
                    url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                }

            }
            else if (id?.ToLower() == "osoba")
            {
                Osoba o = Osoby.GetByNameId.Get(v);
                if (o != null)
                {
                    if (!(await o.NotInterestingToShowAsync()))
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = o.SocialInfoTitle(),
                            body = o.SocialInfoBody(),
                            footer = o.SocialInfoFooter(),
                            subtitle = o.SocialInfoSubTitle(),
                            img = o.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }

            }
            else if (id?.ToLower() == "smlouva")
            {
                Smlouva s = await SmlouvaRepo.LoadAsync(v);
                if (s != null)
                {
                    if (!s.NotInterestingToShow())
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = s.SocialInfoTitle(),
                            body = s.SocialInfoBody(),
                            footer = s.SocialInfoFooter(),
                            subtitle = s.SocialInfoSubTitle(),
                            img = s.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }
            }
            else if (id?.ToLower() == "insolvence")
            {
                var s = (await InsolvenceRepo.LoadFromEsAsync(v, false, true))?.Rizeni;
                if (s != null)
                {
                    if (!s.NotInterestingToShow())
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = s.SocialInfoTitle(),
                            body = s.SocialInfoBody(),
                            footer = s.SocialInfoFooter(),
                            subtitle = s.SocialInfoSubTitle(),
                            img = s.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }

            }
            else if (id?.ToLower() == "dataset")
            {
                var s = DataSet.CachedDatasets.Get(v);
                if (s != null)
                {
                    if (!s.NotInterestingToShow())
                    {
                        var social = new ImageBannerCoreData()
                        {
                            title = await s.SocialInfoTitleAsync(),
                            body = s.SocialInfoBody(),
                            footer = s.SocialInfoFooter(),
                            subtitle = s.SocialInfoSubTitle(),
                            img = s.SocialInfoImageUrl(),
                            color = "blue-dark"
                        };
                        url = mainUrl + GetSocialBannerUrl(social, rat == "1x1", true);
                    }
                }

            }
            else if (id?.ToLower() == "quote")
            {
                url = mainUrl + "/imagebannercore/quote"
                    + "?title=" + System.Net.WebUtility.UrlEncode(t)
                    + "&subtitle=" + System.Net.WebUtility.UrlEncode(st)
                    + "&body=" + System.Net.WebUtility.UrlEncode(b)
                    + "&footer=" + System.Net.WebUtility.UrlEncode(f)
                    + "&img=" + System.Net.WebUtility.UrlEncode(img)
                    + "&color=" + col
                    + "&ratio=" + rat;
                v = Devmasters.Crypto.Hash.ComputeHashToHex(url);
            }
            else if (id?.ToLower() == "kindex")
            {
                data = RemoteUrlFromWebCache.GetBinary(mainUrl + "/kindex/banner/" + v, "kindex-banner-" + v, HttpContext.Request.Query["refresh"] == "1");
            }
            else if (id?.ToLower() == "page" && string.IsNullOrEmpty(v) == false)
            {
                var pageUrl = v;
                string socialTitle = "";
                string socialHtml = "";
                string socialFooter = "";
                string socialSubFooter = "";
                string socialFooterImg = "";
                using (Devmasters.Net.HttpClient.URLContent net = new(pageUrl))
                {
                    net.Timeout = 40000;
                    var cont = net.GetContent().Text;
                    socialTitle = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil
                        .GetRegexGroupValues(cont, @"<meta \s*  property=\""og:hlidac_title\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                        .OrderByDescending(o => o.Length).FirstOrDefault()
                        );
                    socialHtml = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil
                        .GetRegexGroupValues(cont, @"<meta \s*  property=\""og:hlidac_html\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                        .OrderByDescending(o => o.Length).FirstOrDefault()
                        );
                    socialFooter = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil.GetRegexGroupValues(cont, @"<meta \s*  property=\""og:hlidac_footer\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                        .OrderByDescending(o => o.Length).FirstOrDefault()
                        );
                    socialSubFooter = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil.GetRegexGroupValues(cont, @"<meta \s*  property=\""og:hlidac_subfooter\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                        .OrderByDescending(o => o.Length).FirstOrDefault()
                        );
                    socialFooterImg = System.Net.WebUtility.HtmlDecode(
                        Devmasters.RegexUtil.GetRegexGroupValues(cont, @"<meta \s*  property=\""og:hlidac_footerimg\"" \s*  content=\""(?<v>.*)\"" \s* />", "v")
                        .OrderByDescending(o => o.Length).FirstOrDefault()
                        );
                }
                if (string.IsNullOrEmpty(socialHtml))
                    return File(@"content\icons\largetile.png", "image/png");
                else
                    url = mainUrl + "/imagebannercore/quote"
                        + "?title=" + System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialTitle))
                        + "&subtitle=" + System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialSubFooter))
                        + "&body=" + System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialHtml))
                        + "&footer=" + System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialFooter))
                        + "&img=" + System.Net.WebUtility.UrlEncode(System.Net.WebUtility.HtmlDecode(socialFooterImg))
                        + "&color=" + col
                        + "&ratio=" + rat;
            }

            try
            {
                if (data == null && !string.IsNullOrEmpty(url))
                {

                    data = RemoteUrlFromWebCache.GetScreenshot(url, (id?.ToLower() ?? "null") + "-" + rat + "-" + v, HttpContext.Request.Query["refresh"] == "1");
                }
            }
            catch (Exception e)
            {
                Util.Consts.Logger.Error("Manager Save", e);
            }
            if (data == null || data.Length == 0)
                return File(@"content\icons\largetile.png", "image/png");
            else
                return File(data, "image/png");

        }

        private string GetSocialBannerUrl(ImageBannerCoreData si, bool ratio1x1 = false, bool localUrl = true)
        {
            string url = "";
            if (localUrl == false)
                url = "https://www.hlidacstatu.cz";

            url = url + "/imagebannercore/social"
                      + "?title=" + System.Net.WebUtility.UrlEncode(si.title)
                      + "&subtitle=" + System.Net.WebUtility.UrlEncode(si.subtitle)
                      + "&body=" + System.Net.WebUtility.UrlEncode(si.body)
                      + "&footer=" + System.Net.WebUtility.UrlEncode(si.footer)
                      + "&img=" + System.Net.WebUtility.UrlEncode(si.img)
                      + "&ratio=" + (ratio1x1 ? "1x1" : "16x9")
                      + "&color=" + si.color;


            return url;
        }

        public ActionResult Tip(string id)
        {
            string? url;
            using (DbEntities db = new())
            {
                url = db.TipUrl.AsQueryable().Where(m => m.Name == id).Select(m => m.Url).FirstOrDefault();
                url ??= "/";
            }

            try
            {
                var path = "/tip/" + id;
                Visit.AddVisit(path,
                    Visit.IsCrawler(Request.Headers[HeaderNames.UserAgent]) ?
                        Visit.VisitChannel.Crawler : Visit.VisitChannel.Web);
            }
            catch (Exception e)
            {
                Util.Consts.Logger.Info("VisitImg base64 encoding error", e);
            }

            return Redirect(url);
        }
        public ActionResult Status()
        {
            return View(Models.HealthCheckStatusModel.CurrentData.Get());
        }
        public ActionResult Tmp()
        {
            return View();
        }
    }
}