using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HlidacStatu.JobsWeb.Pages
{
    public class KontaktModel : PageModel
    {
        private readonly ILogger<KontaktModel> _logger;

        public KontaktModel(ILogger<KontaktModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}