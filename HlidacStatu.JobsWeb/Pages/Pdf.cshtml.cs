using System.Threading.Tasks;
using HlidacStatu.JobsWeb.Models;
using HlidacStatu.JobsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    public class PdfModel : PageModel
    {
        public YearlyStatisticsGroup.Key? Key { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var ad = await HttpContext.HasAccess();
            if (ad.Access == false)
                return Redirect("/");

            Key = HttpContext.TryFindKey();
            return Page();
        }
    }
}