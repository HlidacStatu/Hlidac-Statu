using System.Threading.Tasks;
using HlidacStatu.JobsWeb.Models;
using HlidacStatu.JobsWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    public class OborModel : PageModel
    {

        public YearlyStatisticsGroup.Key? Key { get; set; }
        public string Obor { get; set; }

        public async Task<IActionResult> OnGetAsync(string id )
        {
            var ad = await HttpContext.HasAccess();
            if (ad.Access == false)
                return Redirect("/");

            Obor = id;
            Key = HttpContext.TryFindKey();
            return Page();
        }
        
    }
}