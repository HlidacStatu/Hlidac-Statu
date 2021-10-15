using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    [Authorize(Roles = "Admin")]
    public class PdfModel : PageModel
    {
        public void OnGet()
        {
            
        }
    }
}