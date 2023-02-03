using System.Threading.Tasks;
using HlidacStatu.Ceny.Models;
using HlidacStatu.Ceny.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.Ceny.Pages
{
    [Authorize(Roles = "Admin")]
    public class JedenJobModel : PageModel
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