﻿using Devmasters.Enums;

using HlidacStatu.Entities;
using HlidacStatu.Lib.Analysis.KorupcniRiziko;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;

using Microsoft.AspNetCore.Mvc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HlidacStatu.Web.Framework;

namespace HlidacStatu.Web.Controllers
{
    public class KindexController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HlidacCache(60*60*12,null,false)]
        public ActionResult DlouhodobaAnalyza()
        {
            return View();
        }


        public ActionResult Detail(string id, int? rok = null, string priv = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Redirect("/");
            }

            if (Util.DataValidators.CheckCZICO(Util.ParseTools.NormalizeIco(id)))
            {
                ViewBag.ICO = id;

                SetViewbagSelectedYear(ref rok);

                (string id, int? rok, string priv) model = (id, rok, priv);
                return View(model);
            }

            return View("Index");
        }

        public async Task<ActionResult> Backup(string id, int? rok = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Redirect("/");
            }

            Backup backup = await KIndexData.GetPreviousVersionAsync(id);

            SetViewbagSelectedYear(ref rok);
            ViewBag.BackupCreated = backup.Created.ToString("dd.MM.yyyy");
            ViewBag.BackupComment = backup.Comment;

            return View(backup.KIndex);

        }

        private void SetViewbagSelectedYear(ref int? rok, int? maxYear = null)
        {
            rok = Consts.FixKindexYear(rok);
            if (maxYear == null && !this.User.IsInRole("Admin"))
            {
                maxYear = Devmasters.ParseText.ToInt(Devmasters.Config.GetWebConfigValue("KIndexMaxYear"));
            }
            if (maxYear.HasValue && rok > maxYear.Value)
                rok = maxYear;
            ViewBag.SelectedYear = rok;
        }

        public async Task<ActionResult> Porovnat(string id, int? rok = null)
        {

            SetViewbagSelectedYear(ref rok);

            var results = new List<SubjectWithKIndexAnnualData>();

            if (string.IsNullOrWhiteSpace(id))
                return View(results);


            foreach (var i in id.Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var f = Firmy.Get(Util.ParseTools.NormalizeIco(i));
                if (f.Valid)
                {
                    SubjectWithKIndexAnnualData data = new SubjectWithKIndexAnnualData()
                    {
                        Ico = f.ICO,
                        Jmeno = f.Jmeno
                    };
                    try
                    {
                        await data.PopulateWithAnnualDataAsync(rok.Value);
                    }
                    catch (Exception)
                    {
                        // chybí ičo v objeku data
                        continue;
                    }
                    results.Add(data);
                }
            }

            return View(results);
        }
        public ActionResult Zebricek(string id, int? rok = null, string group = null, string kraj = null, string part = null)
        {

            //SetViewbagSelectedYear(ref rok, Statistics.KIndexStatTotal.Get().Max(m => m.Rok));
            SetViewbagSelectedYear(ref rok);
            ViewBag.SelectedLadder = id;
            ViewBag.SelectedGroup = group;
            ViewBag.SelectedKraj = kraj;


            Firma.Zatrideni.SubjektyObory oborFromId;
            if (Enum.TryParse(id, true, out oborFromId))
                id = "obor";

            switch (id?.ToLower())
            {
                case "obor":
                    ViewBag.LadderTopic = oborFromId.ToNiceDisplayName();
                    ViewBag.LadderTitle = oborFromId.ToNiceDisplayName() + " podle K–Indexu";
                    break;

                case "nejlepsi":
                    ViewBag.LadderTopic = "Top 100 nejlepších subjektů";
                    ViewBag.LadderTitle = "Top 100 nejlepších subjektů podle K–Indexu";
                    break;

                case "nejhorsi":
                    ViewBag.LadderTopic = "Nejhůře hodnocené úřady a organizace";
                    ViewBag.LadderTitle = "Nejhůře hodnocené úřady a organizace podle K–Indexu";
                    break;

                case "celkovy":
                    //return View("NoAvail");
                    ViewBag.LadderTopic = "Kompletní žebříček úřadů a organizací";
                    ViewBag.LadderTitle = "Kompletní žebříček úřadů a organizací podle K–Indexu";
                    break;

                case "skokani":
                    ViewBag.LadderTitle = "Úřady a organizace, kterým se hodnocení K-Indexu meziročně nejvíce změnilo";
                    break;
                default:
                    break;
            }


            (string id, int rok, string group, string kraj) model = ((string)ViewBag.SelectedLadder, rok!.Value,
                group, kraj);
            return View(model);
        }

        public ActionResult RecalculateFeedback(string email, string txt, string url, string data)
        {
            // create a task, so user doesn't have to wait for anything
            _ = Task.Run(() =>
            {
                var f = Firmy.Get(Util.ParseTools.NormalizeIco(data));
                if (f.Valid)
                {

                    try
                    {
                        //string connectionString = Devmasters.Config.GetWebConfigValue("RabbitMqConnectionString");
                        //if (string.IsNullOrWhiteSpace(connectionString))
                        //    throw new Exception("Missing RabbitMqConnectionString");

                        //var message = new Q.Messages.RecalculateKindex()
                        //{
                        //    Comment = txt,
                        //    Created = DateTime.Now,
                        //    Ico = f.ICO,
                        //    User = this.User.Identity.Name
                        //};

                        //Q.Publisher.QuickPublisher.Publish(message, connectionString);

                        string body = $@"
Žádost o rekalkulaci K-Indexu z hlidacstatu.cz.

Pro firmu:{f.ICO}
Od uzivatele:{email} [{User?.Identity?.Name}] (Emaily by se zde měli shodovat)
ke stránce:{url}

text zpravy: {txt}";
                        Util.SMTPTools.SendSimpleMailToPodpora("Žádost o rekalkulaci K-indexu", body, email);



                    }
                    catch (Exception ex)
                    {
                        Util.Consts.Logger.Fatal($"Problem with SMTP. Message={ex}");
                    }
                }
            });
            return Content("");
        }

        // Used for searching
        public async Task<IActionResult> FindCompany(
            [FromServices] IHttpClientFactory _httpClientFactory,
            string id,
            CancellationToken ctx)
        {
            var autocompleteHost = Devmasters.Config.GetWebConfigValue("AutocompleteEndpoint");
            var autocompletePath = $"/autocomplete/Kindex?q={id}";
            var uri = new Uri($"{autocompleteHost}{autocompletePath}");
            using var client = _httpClientFactory.CreateClient(Constants.DefaultHttpClient);

            try
            {
                var response = await client.GetAsync(uri, ctx);

                return new HttpResponseMessageResult(response);
            }
            catch (Exception ex) when ( ex is OperationCanceledException || ex is TaskCanceledException)
            {
                // canceled by user
                Util.Consts.Logger.Info("Autocomplete canceled by user");
            }
            catch (Exception e)
            {
                Util.Consts.Logger.Warning("Autocomplete API problem.", e, new { id });
            }
            
            return NoContent();
        }

        public async Task<JsonResult> KindexForIco(string id, int? rok = null)
        {
            rok = Consts.FixKindexYear(rok);
            var f = Firmy.Get(Util.ParseTools.NormalizeIco(id));
            if (f.Valid)
            {
                var kidx = await KIndex.GetAsync(Util.ParseTools.NormalizeIco(id));

                if (kidx != null)
                {

                    var radky = kidx.ForYear(rok.Value).KIndexVypocet.Radky
                        .Select(r => new
                        {
                            VelicinaName = r.VelicinaName,
                            Label = KIndexData.KindexImageIcon(KIndexData.DetailInfo.KIndexLabelForPart(r.VelicinaPart, r.Hodnota),
                                "height: 25px",
                                showNone: true,
                                KIndexData.KIndexCommentForPart(r.VelicinaPart, kidx.ForYear(rok.Value))),
                            Value = r.Hodnota.ToString("F2")
                        }).ToList();

                    var result = new
                    {
                        UniqueId = Guid.NewGuid(),
                        Ico = kidx.Ico,
                        Jmeno = Devmasters.TextUtil.ShortenText(kidx.Jmeno, 55),
                        Kindex = KIndexData.KindexImageIcon(kidx.ForYear(rok.Value).KIndexLabel,
                                "height: 40px",
                                showNone: true),
                        Radky = radky,
                        KindexReady = kidx.ForYear(rok.Value).KIndexReady
                    };

                    return Json(result);

                }
            }
            return Json(null);
        }

        public async Task<ActionResult> Feedback(string id)
        {
            if (string.IsNullOrEmpty(id))
                return NotFound();

            var feedback = await KindexFeedback.GetByIdAsync(id);

            if (feedback is null)
                return NotFound();

            return View(feedback);
        }

        //todo: What are we going to do with this?
        public async Task<ActionResult> Debug(string id, string ico = "", int? rok = null)
        {
            if (
                !(User.IsInRole("Admin") || User.IsInRole("KIndex"))
                )
            {
                return Redirect("/");
            }

            if (string.IsNullOrEmpty(id))
            {
                return View("Debug.Start");
            }

            if (Util.DataValidators.CheckCZICO(Util.ParseTools.NormalizeIco(id)))
            {
                KIndexData kdata = await KIndex.GetAsync(Util.ParseTools.NormalizeIco(id));
                ViewBag.ICO = id;
                return View("Debug", kdata);
            }
            else if (id?.ToLower() == "porovnat")
            {
                List<KIndexData> kdata = new List<KIndexData>();

                foreach (var i in ico.Split(new char[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var f = Firmy.Get(Util.ParseTools.NormalizeIco(i));
                    if (f.Valid)
                    {
                        var kidx = await KIndex.GetAsync(Util.ParseTools.NormalizeIco(i));
                        if (kidx != null)
                            kdata.Add(kidx);
                    }
                }
                return View("Debug.Porovnat", kdata);
            }
            else
                return NotFound();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult> PercentileBanner(string id, int? part = null, int? rok = null)
        {
            rok = Consts.FixKindexYear(rok);
            var kidx = await KIndex.GetAsync(id);
            if (kidx != null)
            {

                Statistics stat = Statistics.GetStatistics(rok.Value);

                KIndexData.KIndexParts? kpart = (KIndexData.KIndexParts?)part;
                if (kpart.HasValue)
                {
                    var val = kidx.ForYear(rok.Value)?.KIndexVypocet?.Radky?.FirstOrDefault(m => m.VelicinaPart == kpart.Value)?.Hodnota ?? 0;

                    return Content(
                        new KIndexGenerator.PercentileBanner(
                            val,
                            stat.Percentil(1, kpart.Value),
                            stat.Percentil(10, kpart.Value),
                            stat.Percentil(25, kpart.Value),
                            stat.Percentil(50, kpart.Value),
                            stat.Percentil(75, kpart.Value),
                            stat.Percentil(90, kpart.Value),
                            stat.Percentil(99, kpart.Value),
                            Connectors.Init.WebAppDataPath).Svg()
                        , "image/svg+xml");
                }
                else
                {
                    var val = kidx?.ForYear(rok.Value)?.KIndex ?? 0;

                    return Content(
                        new KIndexGenerator.PercentileBanner(
                            val,
                            stat.Percentil(1),
                            stat.Percentil(10),
                            stat.Percentil(25),
                            stat.Percentil(50),
                            stat.Percentil(75),
                            stat.Percentil(90),
                            stat.Percentil(99),
                            Connectors.Init.WebAppDataPath).Svg()
                        , "image/svg+xml");

                }

            }
            else
                return Content("", "image/svg+xml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<ActionResult> Banner(string id, int? rok = null)
        {
            var kidx = await KIndex.GetAsync(id);

            byte[] data = null;
            if (kidx != null)
            {
                KIndexData.KIndexLabelValues label;
                Util.InfoFact[] infoFacts;
                int year;
                if (rok is null)
                {
                    label = kidx.LastKIndexLabel(out int? y);
                    year = y.Value;
                    infoFacts = kidx.InfoFacts(year);
                }
                else
                {
                    year = Consts.FixKindexYear(rok);
                    label = kidx.ForYear(year)?.KIndexLabel ?? KIndexData.KIndexLabelValues.None;
                    infoFacts = kidx.InfoFacts(year);
                }


                KIndexGenerator.IndexLabel img = new KIndexGenerator.IndexLabel(Connectors.Init.WebAppDataPath);
                data = img.GenerateImageByteArray(kidx.Jmeno,
                    Util.InfoFact.RenderInfoFacts(infoFacts,
                        3,
                        takeSummary: (label == KIndexData.KIndexLabelValues.None),
                        shuffle: false,
                        " "),
                    label.ToString(),
                    year.ToString());
            }

            return File(data, "image/png");
        }

    }
}