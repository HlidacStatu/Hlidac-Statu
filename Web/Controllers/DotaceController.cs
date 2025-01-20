using System;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;
using HlidacStatu.Web.Filters;
using Microsoft.AspNetCore.Mvc;
using Devmasters.Enums;

namespace HlidacStatu.Web.Controllers
{
    public class DotaceController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> Hledat(Repositories.Searching.SubsidySearchResult model)
        {
            if (model == null || ModelState.IsValid == false)
            {
                return View(new Repositories.Searching.SubsidySearchResult());
            }

            // var aggs = new Nest.AggregationContainerDescriptor<Subsidy>()
            //     .Sum("souhrn", s => s
            //         .Field(f => f.AssumedAmount)
            //
            //     );
            
            var aggs = new Nest.AggregationContainerDescriptor<Subsidy>()
                .Filter("filtered_sum", f => f
                    .Filter(q => q
                        .Term(t => t
                            .Field(f => f.Hints.IsOriginal)
                            .Value(true)
                        )
                    )
                    .Aggregations(subAgg => subAgg
                        .Sum("souhrn", s => s
                            .Field(f => f.AssumedAmount)
                        )
                    )
                );


            var res = await SubsidyRepo.Searching.SimpleSearchAsync(model, anyAggregation: aggs);

            AuditRepo.Add(
                Audit.Operations.UserSearch
                , User?.Identity?.Name
                , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                , "Subsidy"
                , res.IsValid ? "valid" : "invalid"
                , res.Q, res.OrigQuery);


            return View(res);
        }

        public async Task<ActionResult> Detail(string id, bool? r = true)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Redirect("/dotace");
            }
            var dotace = await SubsidyRepo.GetAsync(id);
            if (dotace is null)
            {
                return NotFound();
            }
            if (r == true && dotace.Hints.IsOriginal == false && !string.IsNullOrEmpty(dotace.Hints.OriginalSubsidyId))
            {
                var dotaceOrigExists = await SubsidyRepo.ExistsAsync(dotace.Hints.OriginalSubsidyId);
                if (dotaceOrigExists)
                {
                    return RedirectToAction(this.ControllerContext.ActionDescriptor.ActionName, this.ControllerContext.ActionDescriptor.ControllerName, new { id = dotace.Hints.OriginalSubsidyId });
                }
            }

            return View(dotace);
        }

        [HlidacCache(22*60*60,"",false)]
        public async Task<ActionResult> PoLetech()
        {
            var data = await SubsidyRepo.ReportPoLetechAsync();
            return View(data);
        }

        [HlidacCache(22 * 60 * 60, "rok;ftyp;dtyp", false)]
        public async Task<ActionResult> TopPrijemci(int? rok = null, int? ftyp = null, int? dtyp = null)
        {
            Firma.TypSubjektuEnum? typSubj = null;
            if (Enum.TryParse<Firma.TypSubjektuEnum>(ftyp?.ToString(), out Firma.TypSubjektuEnum t))
                typSubj = t;

            Subsidy.Hint.Type? typDotace = null;
            if (Enum.TryParse<Subsidy.Hint.Type>(dtyp?.ToString(), out Subsidy.Hint.Type td))
                typDotace = td;

            ViewData["rok"] = rok;
            ViewData["ftyp"] = ftyp;
            ViewData["dtyp"] = dtyp;


            var data = await SubsidyRepo.ReportTopPrijemciAsync(rok, typSubj, typDotace);

            return View(data);
        }

        [HlidacCache(22 * 60 * 60, "typdotace;rok", false)]
        public async Task<ActionResult> TopPoskytovatele(int typDotace, int? rok = null)
        {
            var data = await SubsidyRepo.ReportPoskytovatelePoLetechAsync((Subsidy.Hint.Type)typDotace, rok);
            ViewData["rok"] = rok;
            ViewData["typDotace"] = typDotace;
            return View(data);
        }

        [HlidacCache(22 * 60 * 60, "rok;cat", false)]
        public async Task<ActionResult> TopKategorie(int? rok = null, int? cat = null)
        {
            ViewData["rok"] = rok;
            ViewData["cat"] = cat;
            if (cat == null)
            {
                var data = await SubsidyRepo.PoKategoriichAsync(rok, null);
                return View("TopKategoriePrehled",data);

            }
            else
            {
                var data = await SubsidyRepo.ReportPrijemciPoKategoriichAsync(rok, cat);
                return View(data);
            }
        }

        [HlidacCache(22 * 60 * 60, "programname;programcode;rok", false)]
        public async Task<ActionResult> Program(string programName, string programCode)
        {
            var data = await SubsidyRepo.ProgramStatisticAsync(programName, programCode);
            ViewData["programName"] = programName;
            ViewData["programCode"] = programCode;
            return View(data);
        }

        [HlidacCache(22 * 60 * 60, "rok", false)]
        public async Task<ActionResult> DotacniExperti(int? rok = null)
        {
            var data = await SubsidyRepo.DotacniExperti(rok);
            ViewData["rok"] = rok;
            return View(data);
        }

        [HlidacCache(22 * 60 * 60, "typdotace;rok", false)]
        public async Task<ActionResult> TopDotacniProgramy(int typDotace, int? rok = null)
        {
            var data = await SubsidyRepo.TopDotacniProgramy((Subsidy.Hint.Type)typDotace , rok);
            ViewData["rok"] = rok;
            ViewData["typDotace"] = typDotace;
            return View(data);
        }

        [HlidacCache(22 * 60 * 60, "rok", false)]
        public async Task<ActionResult> DotovaniSponzori(int? rok = null)
        {
            var data = await SubsidyRepo.DotovaniSponzori(rok);
            ViewData["rok"] = rok;
            return View(data);
        }
        
        public ActionResult Reporty()
        {
            return View();
        }
    }
}