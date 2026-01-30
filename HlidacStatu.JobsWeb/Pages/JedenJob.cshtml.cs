using System.Threading.Tasks;
using WatchdogAnalytics.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WatchdogAnalytics.Models;

namespace WatchdogAnalytics.Pages
{
    [Authorize(Roles = "Admin")]
    public class JedenJobModel : PageModel
    {
        public YearlyStatisticsGroup.Key? Key { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var ad = await HttpContext.HasAccessAsync();
            if (ad.Access == false)
                return Redirect("/");

            Key = HttpContext.TryFindKey();
            return Page();
        }

    }
}