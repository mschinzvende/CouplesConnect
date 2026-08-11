using CouplesConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CouplesConnect.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ResetPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool IsValidLink { get; private set; }

        public class InputModel
        {
            [Required]
            public string UserId { get; set; } = "";

            [Required]
            public string Code { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            [Compare(nameof(Password))]
            public string ConfirmPassword { get; set; } = "";
        }

        public IActionResult OnGet(string? userId, string? code, string? token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                var separator = token.IndexOf('.');
                if (separator > 0)
                {
                    userId = token[..separator];
                    code = token[(separator + 1)..];
                }
            }

            if (Guid.TryParse(userId, out _) && !string.IsNullOrWhiteSpace(code))
            {
                Input.UserId = userId!;
                Input.Code = code;
                IsValidLink = true;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            IsValidLink = Guid.TryParse(Input.UserId, out _) && !string.IsNullOrWhiteSpace(Input.Code);
            if (!IsValidLink)
                return Page();

            if (!ModelState.IsValid)
                return Page();

            var user = await _userManager.FindByIdAsync(Input.UserId);
            if (user is null)
            {
                ModelState.AddModelError("", "This reset link is invalid or has expired.");
                return Page();
            }

            try
            {
                var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
                var result = await _userManager.ResetPasswordAsync(user, code, Input.Password);
                if (result.Succeeded)
                    return RedirectToPage("/Account/ResetPasswordConfirmation");

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            catch (FormatException)
            {
                ModelState.AddModelError("", "This reset link is invalid or has expired.");
            }

            return Page();
        }
    }
}
