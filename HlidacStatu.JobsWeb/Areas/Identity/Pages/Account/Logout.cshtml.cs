using System.Threading.Tasks;
using HlidacStatu.Entities;
using HlidacStatu.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WatchdogAnalytics.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public LogoutModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGet(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.Information("User logged out.");
            if(string.IsNullOrEmpty(returnUrl))
                return RedirectToAction("Index", "Home");
            else
                return LocalRedirect(returnUrl);
        }

        public async Task<IActionResult> OnPost(string? returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.Information("User logged out.");
            if(string.IsNullOrEmpty(returnUrl))
                return RedirectToAction("Index", "Home");
            else
                return LocalRedirect(returnUrl);
        }
    }
}
