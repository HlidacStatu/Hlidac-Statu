using System.Threading.Tasks;
using WatchdogAnalytics.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WatchdogAnalytics.Models;

namespace WatchdogAnalytics.Pages
{
    public class OborModel : PageModel
    {

        public YearlyStatisticsGroup.Key? Key { get; set; }
        public string Obor { get; set; }

        public async Task<IActionResult> OnGetAsync(string id )
        {
            var ad = await HttpContext.HasAccessAsync();
            if (ad.Access == false)
                return Redirect("/");

            Obor = id;
            Key = HttpContext.TryFindKey();
            return Page();
        }
        
    }
}