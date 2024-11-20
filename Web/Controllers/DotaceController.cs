using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Entities;
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

            var aggs = new Nest.AggregationContainerDescriptor<Subsidy>()
                .Sum("souhrn", s => s
                    .Field(f => f.AssumedAmount)

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

        public async Task<ActionResult> Detail(string id)
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
            return View(dotace);
        }
    }
}