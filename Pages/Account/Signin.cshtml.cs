using CouplesConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Pages.Account
{
    public class SigninModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SigninModel(SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            public bool RememberMe { get; set; }
        }

        public IActionResult OnGet()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return Redirect("/DashboardRedirect");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectAfterAuthentication();
            }

            if (!ModelState.IsValid)
                return Page();

            var result = await _signInManager.PasswordSignInAsync(
                Input.Email,
                Input.Password,
                Input.RememberMe,
                false);

            if (result.Succeeded)
            {
                return RedirectAfterAuthentication();
            }

            if (result.IsNotAllowed)
            {
                var safeReturnUrl = IsMeaningfulLocalUrl(ReturnUrl) ? ReturnUrl : null;
                return RedirectToPage("/Account/SignupConfirmation", new { email = Input.Email, returnUrl = safeReturnUrl });
            }

            ModelState.AddModelError("", "Invalid login attempt");

            return Page();
        }

        private IActionResult RedirectAfterAuthentication()
        {
            // A bare slash is the public landing page, not a post-login destination.
            // Defaulting to DashboardRedirect preserves role-specific destinations.
            return IsMeaningfulLocalUrl(ReturnUrl)
                ? LocalRedirect(ReturnUrl!)
                : Redirect("/DashboardRedirect");
        }

        private bool IsMeaningfulLocalUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl)
                && !string.Equals(returnUrl, "/", StringComparison.Ordinal)
                && Url.IsLocalUrl(returnUrl);
        }
    }
}
