using System.Threading.Tasks;
using HlidacStatu.JobsWeb.Models;
using HlidacStatu.JobsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    [Authorize(Roles = "Admin")]
    public class GraphTestModel : PageModel
    {
        public async Task<IActionResult> OnGetAsync()
        {
            return Page();
        }

    }
}