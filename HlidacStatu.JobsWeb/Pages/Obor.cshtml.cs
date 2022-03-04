using System.Threading.Tasks;
using HlidacStatu.JobsWeb.Models;
using HlidacStatu.JobsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    [Authorize()]
    public class OborModel : PageModel
    {

        public YearlyStatisticsGroup.Key? Key { get; set; }
        public string Obor { get; set; }

        public async Task<IActionResult> OnGetAsync(string id )
        {
            if (await HttpContext.HasAccess() == false)
                return Redirect("/");

            Obor = id;
            Key = HttpContext.TryFindKey();
            return Page();
        }
        
    }
}