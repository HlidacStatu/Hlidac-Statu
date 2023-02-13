using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.Ceny.Pages
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