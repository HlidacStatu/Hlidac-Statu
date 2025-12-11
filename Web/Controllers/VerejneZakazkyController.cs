using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;

using Microsoft.AspNetCore.Mvc;

using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Connectors;
using HlidacStatu.Extensions;
using HlidacStatu.Lib.Web.UI.Attributes;
using HlidacStatu.LibCore.Filters;

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

        public async Task<ActionResult> Zakazka(string id)
        {
            if (string.IsNullOrEmpty(id))
                return new NotFoundResult();

            var vz = await VerejnaZakazkaRepo.LoadFromESAsync(id);
            if (vz == null)
                return new NotFoundResult();

            if (!string.IsNullOrEmpty(Request.Query["qs"]))
            {
                var findSm = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(
                    $"_id:\"{vz.Id}\" AND ({Request.Query["qs"]})",
                    new string[] { }, 1, 1,
                    ((int)SmlouvaRepo.Searching.OrderResult.FastestForScroll).ToString(), withHighlighting: true);
                if (findSm.Total > 0)
                    ViewBag.Highlighting = findSm.ElasticResults.Hits.First().Highlight;
            }

            return View(vz);
        }
        
        public async Task<ActionResult> TextDokumentu(string id, string sha)
        {
            var vz = await VerejnaZakazkaRepo.LoadFromESAsync(id);
            if (vz is null || string.IsNullOrWhiteSpace(sha) || vz.Dokumenty?.Any(d => d.Sha256Checksum == sha) == false)
                return new NotFoundResult();
            
            return View(vz);
        }

        public async Task<ActionResult> Priloha(string id, string storageId)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(storageId))
                return NotFound();
            
            
            var zakazka = await VerejnaZakazkaRepo.LoadFromESAsync(id);
            if (zakazka is null)
                return NotFound();

            Entities.VZ.VerejnaZakazka.Document? document = zakazka.Dokumenty.FirstOrDefault(d => d.GetHlidacStorageId() == storageId);

            if (document is null)
                return NotFound();

            var file = document.GetDocumentLocalCopy();
            
            if (Lib.OCR.DocTools.HasPDFHeader(file))
            {
                return File(file, "application/pdf", string.IsNullOrWhiteSpace(document.Name) ? $"{document.Sha256Checksum}_smlouva.pdf" : document.Name);
            }
            else
                return File(file,
                    string.IsNullOrWhiteSpace(document.ContentType) ? "application/octet-stream" : document.ContentType,
                    string.IsNullOrWhiteSpace(document.Name) ? "priloha" : document.Name);
            
        }


        public async Task<ActionResult> Hledat(Repositories.Searching.VerejnaZakazkaSearchData model)
        {
            if (ModelState.IsValid == false)
                return RedirectToAction("Index");
            if (model == null)
                return View(new Repositories.Searching.VerejnaZakazkaSearchData());

            var res = await VerejnaZakazkaRepo.Searching.SimpleSearchAsync(model);
            AuditRepo.Add(
                    Audit.Operations.UserSearch
                    , User?.Identity?.Name
                    , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                    , "VerejnaZakazka"
                    , res.IsValid ? "valid" : "invalid"
                    , res.Q, res.OrigQuery);

            return View(res);
        }

        [HlidacOutputCache(60 * 10, "id;h;embed", false)]
        public ActionResult CPVs()
        {
            return Content(Newtonsoft.Json.JsonConvert.SerializeObject(
                Validators.CPVKody
                    .Select(kv => new { id = kv.Key, txt = kv.Value })
                ), "application/json");
        }

    }
}