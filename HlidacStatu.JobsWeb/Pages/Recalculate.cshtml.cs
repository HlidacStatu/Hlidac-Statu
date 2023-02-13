using System.Threading.Tasks;
using HlidacStatu.Ceny.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HlidacStatu.Ceny.Pages
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