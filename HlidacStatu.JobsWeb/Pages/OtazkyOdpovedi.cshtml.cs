using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HlidacStatu.JobsWeb.Pages
{
    public class OtazkyOdpovediModel : PageModel
    {
        private readonly ILogger<KontaktModel> _logger;

        public OtazkyOdpovediModel(ILogger<KontaktModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}