using HlidacStatu.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    [Authorize(Roles = "Admin")]
    public class BenchmarkUraduModel : PageModel
    {
        public string Ico { get; set; }
        public string Nazev { get; private set; }
        public void OnGet(string id)
        {
            Ico = id;
            Nazev = FirmaRepo.NameFromIco(Ico, true);
        }
    }
}