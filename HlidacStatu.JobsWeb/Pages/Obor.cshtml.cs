using System.Threading.Tasks;
using HlidacStatu.Ceny.Models;
using HlidacStatu.Ceny.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.Ceny.Pages
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