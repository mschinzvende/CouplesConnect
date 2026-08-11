using CouplesConnect.Models;
using CouplesConnect.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CouplesConnect.Pages.Account
{
    public class SignupConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notifications;
        private readonly ILogger<SignupConfirmationModel> _logger;

        public SignupConfirmationModel(
            UserManager<ApplicationUser> userManager,
            INotificationService notifications,
            ILogger<SignupConfirmationModel> logger)
        {
            _userManager = userManager;
            _notifications = notifications;
            _logger = logger;
        }

        [BindProperty(SupportsGet = true)]
        [EmailAddress]
        public string? Email { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? StatusMessage { get; private set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            if (!string.IsNullOrWhiteSpace(Email))
            {
                var user = await _userManager.FindByEmailAsync(Email);
                if (user != null && !user.EmailConfirmed)
                {
                    try
                    {
                        await SendConfirmationAsync(user);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unable to resend account confirmation for user {UserId}", user.Id);
                    }
                }
            }

            StatusMessage = "If an unconfirmed account exists for that email address, a new confirmation link has been sent.";
            return Page();
        }

        private async Task SendConfirmationAsync(ApplicationUser user)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var confirmationUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = user.Id, code = encodedCode },
                protocol: Request.Scheme)
                ?? throw new InvalidOperationException("Could not create the confirmation URL.");

            await _notifications.SendAccountConfirmationAsync(
                user,
                confirmationUrl,
                $"{user.Id:N}.{encodedCode}");
        }
    }
}
