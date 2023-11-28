using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WatchdogAnalytics.Services;

namespace WatchdogAnalytics.Pages
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