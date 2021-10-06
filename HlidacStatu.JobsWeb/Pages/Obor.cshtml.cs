using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    public class OborModel : PageModel
    {

        public string Obor { get; set; }
        
        public void OnGet(string id)
        {
            Obor = id;
        }
    }
}