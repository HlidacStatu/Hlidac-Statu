using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using HlidacStatu.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace WatchdogAnalytics.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger _logger = Log.ForContext<RegisterModel>();

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string? retUrl_2 { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string? retUrl_2 = null)
        {
            retUrl_2 = retUrl_2;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? retUrl_2 = null)
        {
            retUrl_2 ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = Input.Email, Email = Input.Email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.Information("User created a new account with password.");

                    AspNetUserApiToken.CreateNew(user);

                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    //var callbackUrl = Url.Page(
                    //    "/Account/ConfirmEmail",
                    //    pageHandler: null,
                    //    values: new { area = "Identity", userId = user.Id, code = code, retUrl_2 = retUrl_2 },
                    //    protocol: Request.Scheme);

                    //var email = XLib.Emails.EmailMsg.CreateEmailMsgFromPostalTemplate("Register");
                    //email.Model.CallbackUrl = callbackUrl;
                    //email.To = user.Email;
                    //email.SendMe();
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }


                    //if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    //    {
                    //        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, retUrl_2 = retUrl_2 });
                    //    }
                    //    else
                    //    {
                    //        await _signInManager.SignInAsync(user, isPersistent: false);
                    //        if (retUrl_2 == "/cenypracehlidac")
                    //        {
                    //            return Redirect("https://www.watchdoganalytics.cz");
                    //        }
                    //        return LocalRedirect(retUrl_2);
                    //    }               

                    return Redirect("https://www.watchdoganalytics.cz");

                }

            }
            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
