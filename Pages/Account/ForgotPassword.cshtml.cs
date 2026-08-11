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
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationService _notifications;
        private readonly ILogger<ForgotPasswordModel> _logger;

        public ForgotPasswordModel(
            UserManager<ApplicationUser> userManager,
            INotificationService notifications,
            ILogger<ForgotPasswordModel> logger)
        {
            _userManager = userManager;
            _notifications = notifications;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = "";
        }

        public IActionResult OnGet()
        {
            return User.Identity?.IsAuthenticated == true
                ? Redirect("/DashboardRedirect")
                : Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user != null && user.EmailConfirmed)
            {
                try
                {
                    await SendResetAsync(user);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to send password reset for user {UserId}", user.Id);
                }
            }

            // Do not disclose whether an email address belongs to an account.
            return RedirectToPage("/Account/ForgotPasswordConfirmation");
        }

        private async Task SendResetAsync(ApplicationUser user)
        {
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var resetUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { userId = user.Id, code = encodedCode },
                protocol: Request.Scheme)
                ?? throw new InvalidOperationException("Could not create the password reset URL.");

            await _notifications.SendPasswordResetAsync(
                user,
                resetUrl,
                $"{user.Id:N}.{encodedCode}");
        }
    }
}
