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

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (returnUrl != null)
                {
                    return Redirect(returnUrl);
                }

                else
                {
                    return Redirect("/DashboardRedirect");
                }
                
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
                if (returnUrl != null)
                {
                    return Redirect(returnUrl);
                }

                else
                {
                    return Redirect("/DashboardRedirect");
                }
            }

            ModelState.AddModelError("", "Invalid login attempt");

            return Page();
        }
    }
}