using HlidacStatu.JobsWeb.Models;
using HlidacStatu.JobsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    [Authorize(Roles = "Admin")]
    public class JedenJobModel : PageModel
    {
        public YearlyStatisticsGroup.Key? Key { get; set; }
        
        public void OnGet()
        {
            Key = HttpContext.TryFindKey();
        }
    }
}