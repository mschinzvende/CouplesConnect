using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Pages.Account
{
    public class SignupModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public SignupModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            public string FullName { get; set; }

            public string? Alias { get; set; }

            public string? RelationshipStage { get; set; }

            public string? PrivacyLevel { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Required]
            [DataType(DataType.Password)]
            [Compare("Password")]
            public string ConfirmPassword { get; set; }
        }

        public IActionResult OnGet()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return Redirect("/User/Dashboard");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return Redirect("/User/Dashboard");
            }

            if (!ModelState.IsValid)
                return Page();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = Input.FullName,
                Alias = Input.Alias,
                RelationshipStage = Input.RelationshipStage,
                PrivacyLevel = Input.PrivacyLevel,
                UserName = Input.Email,
                Email = Input.Email,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, Input.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

                var settings = await _context.PlatformSettings.FirstOrDefaultAsync();

                var subscription = new UserSubscription
                {
                    UserId = user.Id,
                    SessionsRemaining = settings.TrialSessions,
                    IsTrial = true,
                    IsActive = true,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(settings.TrialDurationDays)
                };

                _context.UserSubscriptions.Add(subscription);
                await _context.SaveChangesAsync();

                await _signInManager.SignInAsync(user, false);

                return Redirect("/User/Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return Page();
        }
    }
}
