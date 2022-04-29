using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Models;

using Microsoft.AspNetCore.Mvc;

using System.Collections.Generic;
using System.Linq;

namespace HlidacStatu.Web.Controllers
{
    public class InsolvenceController : Controller
    {
        public bool IsLimitedView()
        {
            return Framework.InsolvenceLimitedView.IsLimited(User);
        }

        // GET: Insolvence
        public ActionResult Index()
        {
            var model = new InsolvenceIndexViewModel
            {
                NoveFirmyVInsolvenci = InsolvenceRepo.NewFirmyVInsolvenciAsync(10, IsLimitedView()),
                NoveOsobyVInsolvenci = InsolvenceRepo.NewOsobyVInsolvenciAsync(10, IsLimitedView())
            };

            return View(model);
        }

        public ActionResult Hledat(Repositories.Searching.InsolvenceSearchResult model)
        {
            if (model == null || ModelState.IsValid == false)
            {
                return View(new Repositories.Searching.InsolvenceSearchResult());
            }

            model.LimitedView = IsLimitedView();
            var res = InsolvenceRepo.Searching.SimpleSearchAsync(model);

            AuditRepo.Add(
                Audit.Operations.UserSearch
                , User?.Identity?.Name
                , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                , "Insolvence"
                , res.IsValid ? "valid" : "invalid"
                , res.Q, res.OrigQuery);

            return View(res);
        }

        public ActionResult Rizeni(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new NotFoundResult();
            }

            //show highlighting
            bool showHighliting = !string.IsNullOrEmpty(Request.Query["qs"]);

            var model = InsolvenceRepo.LoadFromEsAsync(id, showHighliting, false);
            if (model == null)
            {
                return new NotFoundResult();
            }

            if (IsLimitedView() && model.Rizeni.OnRadar == false)
                return RedirectToAction("PristupOmezen", new { id = model.Rizeni.UrlId() });

            if (showHighliting)
            {
                var findRizeni =
                    InsolvenceRepo.Searching.SimpleSearchAsync($"_id:\"{model.Rizeni.SpisovaZnacka}\" AND ({Request.Query["qs"]})", 1, 1,
                        0, true);
                if (findRizeni.Total > 0)
                    ViewBag.Highlighting = findRizeni.ElasticResults.Hits.First().Highlight;
            }

            ViewBag.showHighliting = showHighliting;
            return View(model);
        }

        public ActionResult Dokumenty(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new NotFoundResult();
            }

            bool showHighliting = !string.IsNullOrEmpty(Request.Query["qs"]);

            var data = InsolvenceRepo.LoadFromEsAsync(id, showHighliting, false);
            if (data == null)
            {
                return new NotFoundResult();
            }

            if (IsLimitedView() && data.Rizeni.OnRadar == false)
                return RedirectToAction("PristupOmezen", new { id = data.Rizeni.UrlId() });

            IReadOnlyDictionary<string, IReadOnlyCollection<string>> highlighting = null;
            if (showHighliting)
            {
                var findRizeni =
                    InsolvenceRepo.Searching.SimpleSearchAsync($"_id:\"{data.Rizeni.SpisovaZnacka}\" AND ({Request.Query["qs"]})", 1, 1, 0,
                        true);
                if (findRizeni.Total > 0)
                {
                    highlighting = findRizeni.ElasticResults.Hits.First().Highlight;
                }
            }

            return View(new DokumentyViewModel
            {
                SpisovaZnacka = data.Rizeni.SpisovaZnacka,
                UrlId = data.Rizeni.UrlId(),
                Dokumenty = data.Rizeni.Dokumenty.ToArray(),
                HighlightingData = highlighting
            });
        }

        public ActionResult TextDokumentu(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new NotFoundResult();
            }

            var dokument = InsolvenceRepo.LoadDokumentAsync(id, false);
            if (dokument == null)
            {
                return new NotFoundResult();
            }

            if (IsLimitedView() && dokument.Rizeni.OnRadar == false)
                return RedirectToAction("PristupOmezen", new { id = dokument.Rizeni.UrlId() });

            return View(dokument);
        }

        public ActionResult PristupOmezen(string id)
        {
            ViewBag.Id = id;
            return View();
        }
    }
}