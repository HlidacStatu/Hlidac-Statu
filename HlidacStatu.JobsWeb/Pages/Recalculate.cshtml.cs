using System.Threading.Tasks;
using HlidacStatu.JobsWeb.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    public class RecalculateModel : PageModel
    {
        
        public async Task OnPostAsync()
        {
            if(!JobService.IsRecalculating)
                await JobService.Recalculate();
        }
    }
}