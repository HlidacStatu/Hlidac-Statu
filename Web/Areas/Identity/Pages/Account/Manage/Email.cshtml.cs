using HlidacStatu.Entities;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace HlidacStatu.Web.Areas.Identity.Pages.Account.Manage
{
    public partial class EmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public EmailModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        public string Email { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "New email")]
            public string NewEmail { get; set; }
            [Required]
            public string Password { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var email = await _userManager.GetEmailAsync(user);
            Email = email;

            Input = new InputModel
            {
                NewEmail = email,
            };

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostChangeEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }
            var checkPassword = await _userManager.CheckPasswordAsync(user, Input.Password);
            if (checkPassword == false)
            {
                ModelState.AddModelError(string.Empty, "Heslo nesouhlasí.");
                await LoadAsync(user);
                return Page();
            }

            var existingUser = await _userManager.FindByEmailAsync(Input.NewEmail);
                        if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(string.Empty, "Tento email je již na Hlídač státu registrován. Změna emailu proto není možná.");
                await LoadAsync(user);
                return Page();
            }

            var email = await _userManager.GetEmailAsync(user);
            if (Input.NewEmail != email)
            {
                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateChangeEmailTokenAsync(user, Input.NewEmail);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ConfirmEmailChange",
                    pageHandler: null,
                    values: new { userId = userId, email = Input.NewEmail, code = code },
                    protocol: Request.Scheme);

                var emailSender = XLib.Emails.EmailMsg.CreateEmailMsgFromPostalTemplate("ChangeEmail");
                emailSender.Model.CallbackUrl = callbackUrl;
                emailSender.Model.OldEmail = email;
                emailSender.Model.NewEmail = Input.NewEmail;
                emailSender.To = Input.NewEmail;
                emailSender.SendMe();

                var emailSender2 = XLib.Emails.EmailMsg.CreateEmailMsgFromPostalTemplate("ChangeEmailNotice");
                emailSender2.Model.CallbackUrl = callbackUrl;
                emailSender2.Model.OldEmail = email;
                emailSender2.Model.NewEmail = Input.NewEmail;
                emailSender2.To = email;
                emailSender2.SendMe();

                StatusMessage = $"Mail s potvrzením o změně email byl odeslán na {Input.NewEmail} i {email} . Otevřete ho a potvrďte změnu kliknutím na odkaz ve zprávě.";
                return RedirectToPage();
            }

            StatusMessage = "Email nebyl změněn.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code },
                protocol: Request.Scheme);

            var emailSender = XLib.Emails.EmailMsg.CreateEmailMsgFromPostalTemplate("Register");
            emailSender.Model.CallbackUrl = callbackUrl;
            emailSender.To = email;
            emailSender.SendMe();

            StatusMessage = "Verification email sent. Please check your email.";
            return RedirectToPage();
        }
    }
}
