using System.Threading.Tasks;
using HlidacStatu.JobsWeb.Models;
using HlidacStatu.JobsWeb.Services;
using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{

    public class BenchmarkUraduModel : PageModel
    {
        public string Ico { get; set; }
        public string Nazev { get; private set; }
        public YearlyStatisticsGroup.Key? Key { get; set; }
        public async Task<IActionResult> OnGetAsync(string id)
        {
            var ad = await HttpContext.HasAccess();
            if (ad.Access == false)
                return Redirect("/");


            Ico = id;
            Nazev = FirmaRepo.NameFromIco(Ico, true);

            Key = HttpContext.TryFindKey();

            return Page();
        }

    }
}