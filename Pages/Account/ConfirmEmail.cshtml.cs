using CouplesConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace CouplesConnect.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ConfirmEmailModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public bool Confirmed { get; private set; }
        public string Message { get; private set; } = "";
        public string? ReturnUrl { get; private set; }

        public async Task<IActionResult> OnGetAsync(Guid? userId, string? code, string? token, string? returnUrl)
        {
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
            if (!string.IsNullOrWhiteSpace(token))
            {
                var separator = token.IndexOf('.');
                if (separator > 0 && Guid.TryParseExact(token[..separator], "N", out var tokenUserId))
                {
                    userId = tokenUserId;
                    code = token[(separator + 1)..];
                }
            }

            if (userId is null || string.IsNullOrWhiteSpace(code))
            {
                Message = "This confirmation link is incomplete. Request a new one to continue.";
                return Page();
            }

            var user = await _userManager.FindByIdAsync(userId.Value.ToString());
            if (user is null)
            {
                Message = "This confirmation link is no longer valid. Request a new one to continue.";
                return Page();
            }

            if (user.EmailConfirmed)
            {
                Confirmed = true;
                Message = "Your email address has already been confirmed. You can sign in.";
                return Page();
            }

            try
            {
                var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                var result = await _userManager.ConfirmEmailAsync(user, decodedCode);
                Confirmed = result.Succeeded;
                Message = result.Succeeded
                    ? "Your email address is confirmed. You can now sign in to Couples Connect."
                    : "This confirmation link has expired or is invalid. Request a new one to continue.";
            }
            catch (FormatException)
            {
                Message = "This confirmation link is invalid. Request a new one to continue.";
            }

            return Page();
        }
    }
}
