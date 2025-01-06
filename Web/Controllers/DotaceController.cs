using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Repositories;

using Microsoft.AspNetCore.Mvc;

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
        
        public async Task<ActionResult> PoLetech()
        {
            var data = await SubsidyRepo.PoLetechReportAsync();
        
        
            return View(data);
        }    
        
        public async Task<ActionResult> TopPrijemci(int? rok = null)
        {
            var data = await SubsidyRepo.TopPrijemciReportAsync(rok);
            ViewData["rok"] = rok;
            return View(data);
        }
        
        // public async Task<ActionResult> TopPrijemciPoLetech(int rok)
        // {
        //     var data = await SubsidyRepo.TopPrijemciReportAsync(rok);
        //
        //     return View(data);
        // }
    }
}