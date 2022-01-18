using HlidacStatu.JobsWeb.Models;
using HlidacStatu.JobsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    [Authorize()]
    public class SouhrnModel : PageModel
    {
        public YearlyStatisticsGroup.Key? Key { get; set; }
        
        public IActionResult OnGet()
        {
            if (HttpContext.HasAccess() == false)
                return Redirect("/");

            Key = HttpContext.TryFindKey();
            return Page();   
        }
    }
}