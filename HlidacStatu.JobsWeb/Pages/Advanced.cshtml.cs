using System.Threading.Tasks;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WatchdogAnalytics.Models;
using WatchdogAnalytics.Services;

namespace WatchdogAnalytics.Pages
{
    /*[Authorize(Roles = "Admin")]*/
    public class AdvancedModel : PageModel
    {
        public YearlyStatisticsGroup.Key? Key { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {

            var obor_rok = JobService.TryFindKey(HttpContext);

            var ad = await HttpContext.HasAccessAsync();
            if (ad.Access == false)
                return Redirect("/");
            if (ad.AnalyzeLevel < CenyCustomer.AccessDetail.AccessDetailLevel.PRO)
                return Redirect($"/souhrn?{obor_rok.Value.UrlDecodedParams}");

            Key = HttpContext.TryFindKey();
            return Page();
        }

    }
}