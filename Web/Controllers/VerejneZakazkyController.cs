using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;

using Microsoft.AspNetCore.Mvc;

using System.Linq;

namespace HlidacStatu.Web.Controllers
{
    public class VerejneZakazkyController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Napoveda()
        {
            return View();
        }

        public ActionResult Zakazka(string id)
        {
            if (string.IsNullOrEmpty(id))
                return new NotFoundResult();

            var vz = VerejnaZakazkaRepo.LoadFromESAsync(id);
            if (vz == null)
                return new NotFoundResult();

            if (!string.IsNullOrEmpty(Request.Query["qs"]))
            {
                var findSm = VerejnaZakazkaRepo.Searching.SimpleSearchAsync(
                    $"_id:\"{vz.Id}\" AND ({Request.Query["qs"]})",
                    new string[] { }, 1, 1,
                    ((int)SmlouvaRepo.Searching.OrderResult.FastestForScroll).ToString(), withHighlighting: true);
                if (findSm.Total > 0)
                    ViewBag.Highlighting = findSm.ElasticResults.Hits.First().Highlight;
            }

            return View(vz);
        }

        public ActionResult TextDokumentu(string id, string hash)
        {
            var vz = VerejnaZakazkaRepo.LoadFromESAsync(id);
            if (vz == null)
                return new NotFoundResult();
            if (vz.Dokumenty?.Any(d => d.StorageId == hash) == false)
                return new NotFoundResult();

            return View(vz);
        }


        public ActionResult Hledat(Repositories.Searching.VerejnaZakazkaSearchData model)
        {
            if (ModelState.IsValid == false)
                return RedirectToAction("Index");
            if (model == null)
                return View(new Repositories.Searching.VerejnaZakazkaSearchData());

            var res = VerejnaZakazkaRepo.Searching.SimpleSearchAsync(model);
            AuditRepo.Add(
                    Audit.Operations.UserSearch
                    , User?.Identity?.Name
                    , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                    , "VerejnaZakazka"
                    , res.IsValid ? "valid" : "invalid"
                    , res.Q, res.OrigQuery);

            return View(res);
        }




        [HlidacCache(60 * 10, "id;h;embed", false)]
        public ActionResult CPVs()
        {
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                Validators.CPVKody
                    .Select(kv => new { id = kv.Key, txt = kv.Value })
                ), "application/json");
        }

    }
}