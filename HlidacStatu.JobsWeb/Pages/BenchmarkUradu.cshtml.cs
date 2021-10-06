using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    [Authorize(Roles = "Admin")]
    public class BenchmarkUraduModel : PageModel
    {
        public string Ico { get; set; }
        public void OnGet(string id)
        {
            Ico = id;
        }
    }
}