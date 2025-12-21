using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace WatchdogAnalytics.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginWithRecoveryCodeModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger _logger = Log.ForContext<LoginWithRecoveryCodeModel>();

        public LoginWithRecoveryCodeModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string retUrl_2 { get; set; }

        public class InputModel
        {
            [BindProperty]
            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Recovery Code")]
            public string RecoveryCode { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string? retUrl_2 = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            retUrl_2 = retUrl_2;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? retUrl_2 = null)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new InvalidOperationException($"Unable to load two-factor authentication user.");
            }

            var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.Information($"User with ID '{user.Id}' logged in with a recovery code.");
                return LocalRedirect(retUrl_2 ?? Url.Content("~/"));
            }
            if (result.IsLockedOut)
            {
                _logger.Warning($"User with ID '{user.Id}' account locked out.");
                return RedirectToPage("./Lockout");
            }
            else
            {
                _logger.Warning($"Invalid recovery code entered for user with ID '{user.Id}' ");
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return Page();
            }
        }
    }
}
