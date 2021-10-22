using HlidacStatu.JobsWeb.Models;
using HlidacStatu.JobsWeb.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    public class IndexModel : PageModel
    {
        
        public YearlyStatisticsGroup.Key? Key { get; set; }
        
        public void OnGet()
        {
            Key = HttpContext.TryFindKey();
        }
    }
}