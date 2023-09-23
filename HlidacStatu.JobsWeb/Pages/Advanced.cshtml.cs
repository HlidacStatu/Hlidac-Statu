using System.Threading.Tasks;
using HlidacStatu.Ceny.Models;
using HlidacStatu.Ceny.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.Ceny.Pages
{
    /*[Authorize(Roles = "Admin")]*/
    public class AdvancedModel : PageModel
    {
        public YearlyStatisticsGroup.Key? Key { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {

            var obor_rok = JobService.TryFindKey(HttpContext);

            var ad = await HttpContext.HasAccess();
            if (ad.Access == false)
                return Redirect("/");
            if (ad.AnalyzeLevel < Entities.CenyCustomer.AccessDetail.AccessDetailLevel.PRO)
                return Redirect($"/souhrn?{obor_rok.Value.UrlDecodedParams}");

            Key = HttpContext.TryFindKey();
            return Page();
        }

    }
}