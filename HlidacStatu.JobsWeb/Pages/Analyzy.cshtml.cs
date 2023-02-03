using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HlidacStatu.Ceny.Pages
{
    public class AnalyzyModel : PageModel
    {
        private readonly ILogger<KontaktModel> _logger;

        public AnalyzyModel(ILogger<KontaktModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}