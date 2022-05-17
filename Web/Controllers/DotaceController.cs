using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Entities.Dotace;
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

        public async Task<ActionResult> Hledat(Repositories.Searching.DotaceSearchResult model)
        {
            if (model == null || ModelState.IsValid == false)
            {
                return View(new Repositories.Searching.DotaceSearchResult());
            }

            var aggs = new Nest.AggregationContainerDescriptor<Dotace>()
                .Sum("souhrn", s => s
                    .Field(f => f.DotaceCelkem)

                );


            var res = await DotaceRepo.Searching.SimpleSearchAsync(model, anyAggregation: aggs);

            AuditRepo.Add(
                Audit.Operations.UserSearch
                , User?.Identity?.Name
                , HlidacStatu.Util.RealIpAddress.GetIp(HttpContext)?.ToString()
                , "Dotace"
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
            var dotace = await DotaceRepo.GetAsync(id);
            if (dotace is null)
            {
                return NotFound();
            }
            return View(dotace);
        }
    }
}