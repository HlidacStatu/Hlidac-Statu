using System.Threading.Tasks;
using HlidacStatu.JobsWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.JobsWeb.Pages
{
    [Authorize(Roles = "Admin")]
    public class RecalculateModel : PageModel
    {
        
        public async Task OnPostAsync()
        {
            if(!JobService.IsRecalculating)
                await JobService.RecalculateAsync();
        }
    }
}