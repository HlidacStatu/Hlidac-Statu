using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System;
using Microsoft.AspNetCore.Mvc;

namespace WatchdogAnalytics.Pages
{
    [IgnoreAntiforgeryToken(Order = 1001)]
    public class PrivacyModel : PageModel
    {
        private readonly ILogger<KontaktModel> _logger;

        public PrivacyModel(ILogger<KontaktModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }

 
    }
}