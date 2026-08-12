using CouplesConnect.Data;
using CouplesConnect.Models;
using CouplesConnect.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CouplesConnect.Pages.Account
{
    public class SignupModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notifications;
        private readonly ILogger<SignupModel> _logger;

        public SignupModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            INotificationService notifications,
            ILogger<SignupModel> logger)
        {
            _userManager = userManager;
            _context = context;
            _notifications = notifications;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            public string FullName { get; set; }

            public string? Alias { get; set; }

            public string? RelationshipStage { get; set; }

            public string? PrivacyLevel { get; set; }
            [Required]
            public string? Phone { get; set; }

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

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }

                return Redirect("/User/Dashboard");
            }

            if (!ModelState.IsValid)
                return Page();

            var existingUser = await _userManager.FindByEmailAsync(Input.Email);
            if (existingUser != null)
            {
                if (!existingUser.EmailConfirmed)
                {
                    var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
                    return RedirectToPage("/Account/SignupConfirmation", new { email = existingUser.Email, returnUrl = safeReturnUrl });
                }

                ModelState.AddModelError("", "An account already exists for this email address. Please sign in.");
                return Page();
            }

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = Input.FullName,
                Alias = Input.Alias,
                RelationshipStage = Input.RelationshipStage,
                PrivacyLevel = Input.PrivacyLevel,
                UserName = Input.Email,
                Email = Input.Email,
                CreatedAt = DateTime.UtcNow,
                Phone= Input.Phone
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

                var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null;

                try
                {
                    await SendConfirmationAsync(user, safeReturnUrl);
                }
                catch (Exception ex)
                {
                    // The account remains pending confirmation and the user can resend
                    // the link. A delivery outage must not create a duplicate account.
                    _logger.LogError(ex, "Unable to send account confirmation for user {UserId}", user.Id);
                }

                return RedirectToPage("/Account/SignupConfirmation", new { email = user.Email, returnUrl = safeReturnUrl });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return Page();
        }

        private async Task SendConfirmationAsync(ApplicationUser user, string? returnUrl)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var confirmationUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = user.Id, code = encodedCode, returnUrl },
                protocol: Request.Scheme)
                ?? throw new InvalidOperationException("Could not create the confirmation URL.");

            var whatsAppButtonToken = $"{user.Id:N}.{encodedCode}";
            await _notifications.SendAccountConfirmationAsync(user, confirmationUrl, whatsAppButtonToken);
        }
    }
}
