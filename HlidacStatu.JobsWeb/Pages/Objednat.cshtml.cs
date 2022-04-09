using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace HlidacStatu.JobsWeb.Pages
{
    public class ObjednatModel : PageModel
    {
        private readonly ILogger<ObjednatModel> _logger;

        public ObjednatModel(ILogger<ObjednatModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}