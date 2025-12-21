using HlidacStatu.Entities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger = Log.ForContext<LogoutModel>();

        public LogoutModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGet(string? retUrl_2 = null)
        {
            await _signInManager.SignOutAsync();
            _logger.Information("User logged out.");
            if(string.IsNullOrEmpty(retUrl_2))
                return RedirectToAction("Index", "Home");
            else
                return LocalRedirect(retUrl_2);
        }

        public async Task<IActionResult> OnPost(string? retUrl_2 = null)
        {
            await _signInManager.SignOutAsync();
            _logger.Information("User logged out.");
            if(string.IsNullOrEmpty(retUrl_2))
                return RedirectToAction("Index", "Home");
            else
                return LocalRedirect(retUrl_2);
        }
    }
}
