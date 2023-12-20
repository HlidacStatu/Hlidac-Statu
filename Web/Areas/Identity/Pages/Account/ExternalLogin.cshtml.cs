using HlidacStatu.Entities;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace HlidacStatu.Web.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        
        private readonly ILogger _logger = Log.ForContext<ExternalLoginModel>();

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ProviderDisplayName { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public IActionResult OnGetAsync()
        {
            return RedirectToPage("./Login");
        }

        public IActionResult OnPost(string provider, string? returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return new ChallengeResult(provider, properties);
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.Information($"{info.Principal.Identity.Name} logged in with {info.LoginProvider} provider.");
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                var email = FindEmailInClaims(info);
                //if it has email - then create user
                if (string.IsNullOrEmpty(email))
                {
                    ErrorMessage = "Error loading email value from external login.";
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }
                
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null) //we need to create a new user
                {
                    user = new ApplicationUser { UserName = email, Email = email };
                    var userCreateResult = await _userManager.CreateAsync(user);
                    if (!userCreateResult.Succeeded)
                    {
                        ErrorMessage = "User couldn't be created.";
                        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                    }
                    
                    // send confirmation email
                    _logger.Information($"User created an account using {info.LoginProvider} provider.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code },
                        protocol: Request.Scheme);

                    var emailTemplate = XLib.Emails.EmailMsg.CreateEmailMsgFromPostalTemplate("Register");
                    emailTemplate.Model.CallbackUrl = callbackUrl;
                    emailTemplate.To = user.Email;
                    emailTemplate.SendMe();
                }

                var loginres = await _signInManager.UserManager.AddLoginAsync(user, info); // store user token
                await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

                return LocalRedirect(returnUrl);
            }
        }

        public IActionResult OnPostConfirmationAsync(string? returnUrl = null)
        {
            //no confirmation needed
            return NotFound("this action is disabled");
        }
        
        // diferent login providers can have different claim names
        private string? FindEmailInClaims(ExternalLoginInfo info)
        {
            return info.LoginProvider switch
            {
                "mojeid" => info.Principal.FindFirstValue("email"),
                _ => info.Principal.FindFirstValue(ClaimTypes.Email)
            };
        }
    }
}
