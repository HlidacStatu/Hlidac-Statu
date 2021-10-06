using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    public class BenchmarkUraduModel : PageModel
    {
        public string Ico { get; set; }
        public void OnGet(string id)
        {
            Ico = id;
        }
    }
}