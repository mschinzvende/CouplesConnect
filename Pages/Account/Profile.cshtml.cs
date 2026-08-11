using CouplesConnect.Models;
using CouplesConnect.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly INotificationService _notifications;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileModel> _logger;

        public ProfileModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            INotificationService notifications,
            IWebHostEnvironment environment,
            ILogger<ProfileModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _notifications = notifications;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new();

        [BindProperty]
        public IFormFile? ProfileImage { get; set; }

        [BindProperty]
        public PasswordInputModel Password { get; set; } = new();

        public string ProfileImageUrl { get; private set; } = "/theme/images/faces/face8.jpg";

        public class ProfileInputModel
        {
            [Required]
            [Display(Name = "Full name")]
            public string FullName { get; set; } = "";

            public string? Alias { get; set; }

            [Required]
            [Phone]
            public string Phone { get; set; } = "";

            [Display(Name = "Relationship stage")]
            public string? RelationshipStage { get; set; }

            [Display(Name = "Privacy preference")]
            public string? PrivacyLevel { get; set; }

            public string Email { get; set; } = "";
        }

        public class PasswordInputModel
        {
            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Current password")]
            public string CurrentPassword { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "New password")]
            public string NewPassword { get; set; } = "";

            [Required]
            [DataType(DataType.Password)]
            [Compare(nameof(NewPassword))]
            [Display(Name = "Confirm new password")]
            public string ConfirmPassword { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Challenge();

            PopulateProfile(user);
            return Page();
        }

        public async Task<IActionResult> OnPostSaveProfileAsync()
        {
            RemovePasswordValidation();
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Challenge();

            if (!ModelState.IsValid)
            {
                PopulateProfile(user, preserveInput: true);
                return Page();
            }

            user.FullName = Input.FullName.Trim();
            user.Alias = Input.Alias?.Trim();
            user.Phone = Input.Phone.Trim();
            user.RelationshipStage = Input.RelationshipStage;
            user.PrivacyLevel = Input.PrivacyLevel;

            if (ProfileImage is { Length: > 0 })
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var extension = Path.GetExtension(ProfileImage.FileName).ToLowerInvariant();

                if (ProfileImage.Length > 5 * 1024 * 1024 || !allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(nameof(ProfileImage), "Upload a JPG, PNG, or WebP image up to 5 MB.");
                    PopulateProfile(user, preserveInput: true);
                    return Page();
                }

                var avatarDirectory = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(avatarDirectory);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(avatarDirectory, fileName);

                await using var stream = new FileStream(filePath, FileMode.CreateNew);
                await ProfileImage.CopyToAsync(stream);
                user.ProfileImageUrl = $"/uploads/avatars/{fileName}";
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                PopulateProfile(user, preserveInput: true);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["ProfileStatus"] = "Your profile has been updated.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostChangePasswordAsync()
        {
            RemoveProfileValidation();
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Challenge();

            if (!ModelState.IsValid)
            {
                PopulateProfile(user);
                return Page();
            }

            var result = await _userManager.ChangePasswordAsync(user, Password.CurrentPassword, Password.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                PopulateProfile(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            try
            {
                await _notifications.SendPasswordChangedAlertAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send password-changed alert for user {UserId}", user.Id);
            }

            TempData["PasswordStatus"] = "Your password has been changed. We sent a security notification to your email and WhatsApp.";
            return RedirectToPage();
        }

        private void PopulateProfile(ApplicationUser user, bool preserveInput = false)
        {
            ProfileImageUrl = string.IsNullOrWhiteSpace(user.ProfileImageUrl)
                ? "/theme/images/faces/face8.jpg"
                : user.ProfileImageUrl;

            if (preserveInput)
            {
                Input.Email = user.Email ?? "";
                return;
            }

            Input = new ProfileInputModel
            {
                FullName = user.FullName,
                Alias = user.Alias,
                Phone = user.Phone ?? "",
                RelationshipStage = user.RelationshipStage,
                PrivacyLevel = user.PrivacyLevel,
                Email = user.Email ?? ""
            };
        }

        private void RemovePasswordValidation()
        {
            foreach (var key in ModelState.Keys.Where(key => key.StartsWith("Password.")))
                ModelState.Remove(key);
        }

        private void RemoveProfileValidation()
        {
            foreach (var key in ModelState.Keys.Where(key => key.StartsWith("Input.") || key == nameof(ProfileImage)))
                ModelState.Remove(key);
        }
    }
}
