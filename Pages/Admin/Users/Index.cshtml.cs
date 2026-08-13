using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using CouplesConnect.Data;
using CouplesConnect.Models;
using CouplesConnect.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.Users
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private static readonly int[] AllowedPageSizes = [25, 50, 100];
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly INotificationService _notifications;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            INotificationService notifications,
            ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _notifications = notifications;
            _logger = logger;
        }

        public List<ManagedUserViewModel> Users { get; private set; } = new();
        public List<string> AvailableRoles { get; private set; } = new();
        public int ActiveUserCount { get; private set; }
        public int AdministratorCount { get; private set; }
        public int UnconfirmedEmailCount { get; private set; }
        public int TotalUserCount { get; private set; }
        public int TotalPages { get; private set; }
        public int FirstUserNumber { get; private set; }
        public int LastUserNumber { get; private set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 25;

        public IReadOnlyList<int> PageSizeOptions => AllowedPageSizes;

        public sealed class ManagedUserViewModel
        {
            public Guid Id { get; init; }
            public string FullName { get; init; } = "";
            public string Email { get; init; } = "";
            public string? Phone { get; init; }
            public DateTime CreatedAt { get; init; }
            public bool EmailConfirmed { get; init; }
            public bool IsLockedOut { get; init; }
            public bool CanDelete { get; init; }
            public List<string> Roles { get; init; } = new();
        }

        public async Task OnGetAsync()
        {
            AvailableRoles = await GetAvailableRolesAsync();
            NormalizePaging();

            IQueryable<ApplicationUser> usersQuery = _context.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(Search))
            {
                Search = Search.Trim();
                var normalizedSearch = Search.ToUpperInvariant();
                var matchingRoleUserIds =
                    from userRole in _context.UserRoles.AsNoTracking()
                    join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                    where role.NormalizedName != null && role.NormalizedName.Contains(normalizedSearch)
                    select userRole.UserId;

                usersQuery = usersQuery.Where(user =>
                    user.FullName.Contains(Search) ||
                    (user.Email != null && user.Email.Contains(Search)) ||
                    (user.Phone != null && user.Phone.Contains(Search)) ||
                    (user.PhoneNumber != null && user.PhoneNumber.Contains(Search)) ||
                    matchingRoleUserIds.Contains(user.Id));
            }

            TotalUserCount = await usersQuery.CountAsync();
            TotalPages = Math.Max(1, (int)Math.Ceiling(TotalUserCount / (double)PageSize));
            PageNumber = Math.Min(PageNumber, TotalPages);
            FirstUserNumber = TotalUserCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
            LastUserNumber = Math.Min(PageNumber * PageSize, TotalUserCount);

            var users = await usersQuery
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Email)
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var pageUserIds = users.Select(user => user.Id).ToList();

            var roleAssignments = await (
                    from userRole in _context.UserRoles.AsNoTracking()
                    join role in _context.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                    where pageUserIds.Contains(userRole.UserId)
                    select new { userRole.UserId, RoleName = role.Name ?? "" })
                .ToListAsync();

            var rolesByUser = roleAssignments
                .GroupBy(item => item.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(item => item.RoleName).OrderBy(name => name).ToList());

            var usersWithPreservedActivity = await GetUsersWithPreservedActivityAsync(pageUserIds);
            var currentUserId = GetCurrentUserId();
            var adminRoleId = await GetRoleIdAsync("Admin");
            AdministratorCount = adminRoleId is null
                ? 0
                : await _context.UserRoles.CountAsync(userRole => userRole.RoleId == adminRoleId.Value);

            Users = users.Select(user =>
            {
                var roles = rolesByUser.GetValueOrDefault(user.Id, new List<string>());
                return new ManagedUserViewModel
                {
                    Id = user.Id,
                    FullName = string.IsNullOrWhiteSpace(user.FullName) ? "Unnamed user" : user.FullName,
                    Email = user.Email ?? "",
                    Phone = user.Phone ?? user.PhoneNumber,
                    CreatedAt = user.CreatedAt,
                    EmailConfirmed = user.EmailConfirmed,
                    IsLockedOut = user.LockoutEnd is { } lockoutEnd && lockoutEnd > DateTimeOffset.UtcNow,
                    CanDelete = user.Id != currentUserId && !usersWithPreservedActivity.Contains(user.Id) && !(roles.Contains("Admin") && AdministratorCount <= 1),
                    Roles = roles
                };
            }).ToList();

            var now = DateTimeOffset.UtcNow;
            ActiveUserCount = await _context.Users.CountAsync(user => user.LockoutEnd == null || user.LockoutEnd <= now);
            UnconfirmedEmailCount = await _context.Users.CountAsync(user => !user.EmailConfirmed);
        }

        public async Task<IActionResult> OnPostCreateAsync(
            string? fullName,
            string? email,
            string? phone,
            List<string>? roles)
        {
            var selectedRoles = await ValidateRolesAsync(roles);
            if (string.IsNullOrWhiteSpace(fullName))
                return Error("A full name is required.");
            if (!IsValidEmail(email))
                return Error("Enter a valid email address.");
            if (selectedRoles is null)
                return Error("Select at least one valid role.");

            var normalizedEmail = email!.Trim();
            if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
                return Error("A user with this email address already exists.");

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FullName = fullName.Trim(),
                UserName = normalizedEmail,
                Email = normalizedEmail,
                EmailConfirmed = true,
                Phone = NormalizePhone(phone),
                PhoneNumber = NormalizePhone(phone),
                LockoutEnabled = true,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, CreateOneTimePassword());
            if (!createResult.Succeeded)
                return Error(FormatErrors(createResult));

            var addRolesResult = await _userManager.AddToRolesAsync(user, selectedRoles);
            if (!addRolesResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return Error(FormatErrors(addRolesResult));
            }

            try
            {
                await SendAccountSetupAsync(user, selectedRoles);
                TempData["Success"] = $"{user.FullName}'s account was created and a password setup link was sent.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send account setup notification for user {UserId}", user.Id);
                TempData["Warning"] = $"{user.FullName}'s account was created, but the setup notification could not be sent. Use Reset password to send another link.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(
            Guid userId,
            string? fullName,
            string? email,
            string? phone,
            List<string>? roles)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Error("The user could not be found.");
            if (string.IsNullOrWhiteSpace(fullName))
                return Error("A full name is required.");
            if (!IsValidEmail(email))
                return Error("Enter a valid email address.");

            var selectedRoles = await ValidateRolesAsync(roles);
            if (selectedRoles is null)
                return Error("Select at least one valid role.");

            var existingRoles = await _userManager.GetRolesAsync(user);
            var roleGuardMessage = await ValidateAdministratorChangeAsync(user, existingRoles, selectedRoles);
            if (roleGuardMessage is not null)
                return Error(roleGuardMessage);

            var normalizedEmail = email!.Trim();
            var emailChanged = !string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase);
            var rolesChanged = !existingRoles.OrderBy(role => role).SequenceEqual(selectedRoles.OrderBy(role => role));

            user.FullName = fullName.Trim();
            user.Phone = NormalizePhone(phone);
            user.PhoneNumber = user.Phone;
            user.UserName = normalizedEmail;
            user.Email = normalizedEmail;
            if (emailChanged)
                user.EmailConfirmed = false;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                return Error(FormatErrors(updateResult));

            var rolesResult = await ReplaceRolesAsync(user, existingRoles, selectedRoles);
            if (!rolesResult.Succeeded)
                return Error(FormatErrors(rolesResult));

            if (emailChanged)
            {
                try
                {
                    await SendEmailConfirmationAsync(user);
                    TempData["Success"] = $"{user.FullName}'s details were updated. A verification link was sent to the new email address.";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to send updated-email confirmation for user {UserId}", user.Id);
                    TempData["Warning"] = $"{user.FullName}'s details were updated, but the verification notification could not be sent.";
                }
            }
            else
            {
                TempData["Success"] = rolesChanged
                    ? $"{user.FullName}'s details and access roles were updated."
                    : $"{user.FullName}'s details were updated.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendPasswordResetAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Error("The user could not be found.");
            if (string.IsNullOrWhiteSpace(user.Email) && string.IsNullOrWhiteSpace(user.Phone))
                return Error("This user has no email address or WhatsApp number for a reset notification.");

            try
            {
                await SendPasswordResetAsync(user);
                TempData["Success"] = $"A password reset link was sent to {user.FullName}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send administrator-initiated password reset for user {UserId}", user.Id);
                return Error("The password reset link could not be sent. Check the notification settings and try again.");
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSetPasswordAsync(Guid userId, string? newPassword, string? confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8)
                return Error("Use a password of at least 8 characters.");
            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
                return Error("The new password and confirmation do not match.");

            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Error("The user could not be found.");

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
                return Error(FormatErrors(result));

            try
            {
                await _notifications.SendPasswordChangedAlertAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send password-changed alert after administrator update for user {UserId}", user.Id);
                TempData["Warning"] = "The password was changed, but the security notification could not be sent.";
                return RedirectToPage();
            }

            TempData["Success"] = $"{user.FullName}'s password was changed and a security notification was sent.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAccessAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Error("The user could not be found.");
            if (user.Id == GetCurrentUserId())
                return Error("Use your profile or another administrator to change your own account access.");

            var isLockedOut = user.LockoutEnd is { } lockoutEnd && lockoutEnd > DateTimeOffset.UtcNow;
            var roles = await _userManager.GetRolesAsync(user);
            if (!isLockedOut && roles.Contains("Admin") && await IsOnlyAdministratorAsync(user.Id))
                return Error("The only administrator account cannot be disabled.");

            await _userManager.SetLockoutEnabledAsync(user, true);
            var result = await _userManager.SetLockoutEndDateAsync(
                user,
                isLockedOut ? null : DateTimeOffset.UtcNow.AddYears(100));
            if (!result.Succeeded)
                return Error(FormatErrors(result));

            TempData["Success"] = isLockedOut
                ? $"{user.FullName}'s account access was restored."
                : $"{user.FullName}'s account access was disabled.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return Error("The user could not be found.");
            if (user.Id == GetCurrentUserId())
                return Error("You cannot delete your own administrator account.");

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin") && await IsOnlyAdministratorAsync(user.Id))
                return Error("The only administrator account cannot be deleted.");

            if (await HasPreservedActivityAsync(user.Id))
                return Error("This user has activity that must be retained. Disable access instead of deleting the account.");

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
                return Error(FormatErrors(result));

            TempData["Success"] = $"{user.FullName}'s account was permanently deleted.";
            return RedirectToPage();
        }

        private async Task<List<string>> GetAvailableRolesAsync()
        {
            return await _roleManager.Roles
                .Where(role => role.Name != null)
                .OrderBy(role => role.Name)
                .Select(role => role.Name!)
                .ToListAsync();
        }

        private async Task<List<string>?> ValidateRolesAsync(List<string>? roles)
        {
            var selectedRoles = roles?
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            if (selectedRoles.Count == 0)
                return null;

            var availableRoles = await GetAvailableRolesAsync();
            if (selectedRoles.Any(role => !availableRoles.Contains(role, StringComparer.OrdinalIgnoreCase)))
                return null;

            return selectedRoles;
        }

        private async Task<IdentityResult> ReplaceRolesAsync(
            ApplicationUser user,
            IEnumerable<string> existingRoles,
            IEnumerable<string> selectedRoles)
        {
            var toRemove = existingRoles.Except(selectedRoles, StringComparer.OrdinalIgnoreCase).ToList();
            if (toRemove.Count > 0)
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, toRemove);
                if (!removeResult.Succeeded)
                    return removeResult;
            }

            var toAdd = selectedRoles.Except(existingRoles, StringComparer.OrdinalIgnoreCase).ToList();
            return toAdd.Count == 0
                ? IdentityResult.Success
                : await _userManager.AddToRolesAsync(user, toAdd);
        }

        private async Task<string?> ValidateAdministratorChangeAsync(
            ApplicationUser user,
            IEnumerable<string> existingRoles,
            IEnumerable<string> selectedRoles)
        {
            if (!existingRoles.Contains("Admin") || selectedRoles.Contains("Admin"))
                return null;

            if (user.Id == GetCurrentUserId())
                return "You cannot remove your own Administrator role.";
            if (await IsOnlyAdministratorAsync(user.Id))
                return "The only administrator must retain the Administrator role.";

            return null;
        }

        private async Task<bool> IsOnlyAdministratorAsync(Guid userId)
        {
            var adminRoleId = await GetRoleIdAsync("Admin");
            if (adminRoleId is null)
                return false;

            var isAdministrator = await _context.UserRoles.AnyAsync(userRole =>
                userRole.UserId == userId && userRole.RoleId == adminRoleId.Value);
            if (!isAdministrator)
                return false;

            return await _context.UserRoles.CountAsync(userRole => userRole.RoleId == adminRoleId.Value) <= 1;
        }

        private Task<Guid?> GetRoleIdAsync(string roleName)
        {
            return _context.Roles
                .Where(role => role.NormalizedName == roleName.ToUpper())
                .Select(role => (Guid?)role.Id)
                .FirstOrDefaultAsync();
        }

        private async Task<HashSet<Guid>> GetUsersWithPreservedActivityAsync(IReadOnlyCollection<Guid> pageUserIds)
        {
            var userIds = new HashSet<Guid>();
            if (pageUserIds.Count == 0)
                return userIds;

            userIds.UnionWith(await _context.Bookings.Where(booking => pageUserIds.Contains(booking.UserId)).Select(booking => booking.UserId).ToListAsync());
            userIds.UnionWith(await _context.Bookings.Where(booking => pageUserIds.Contains(booking.CounselorId)).Select(booking => booking.CounselorId).ToListAsync());
            userIds.UnionWith(await _context.BookingParticipants
                .Where(participant => participant.UserId.HasValue && pageUserIds.Contains(participant.UserId.Value))
                .Select(participant => participant.UserId!.Value)
                .ToListAsync());
            userIds.UnionWith(await _context.Counselors.Where(counselor => pageUserIds.Contains(counselor.UserId)).Select(counselor => counselor.UserId).ToListAsync());
            userIds.UnionWith(await _context.CounselorProfiles.Where(profile => pageUserIds.Contains(profile.UserId)).Select(profile => profile.UserId).ToListAsync());
            userIds.UnionWith(await _context.Feedback.Where(feedback => pageUserIds.Contains(feedback.UserId)).Select(feedback => feedback.UserId).ToListAsync());
            userIds.UnionWith(await _context.ReflectionNotes.Where(note => pageUserIds.Contains(note.UserId)).Select(note => note.UserId).ToListAsync());
            userIds.UnionWith(await _context.SessionFeedbacks.Where(feedback => pageUserIds.Contains(feedback.UserId)).Select(feedback => feedback.UserId).ToListAsync());
            userIds.UnionWith(await _context.SupportTickets.Where(ticket => pageUserIds.Contains(ticket.UserId)).Select(ticket => ticket.UserId).ToListAsync());
            userIds.UnionWith(await _context.UserGoals.Where(goal => pageUserIds.Contains(goal.UserId)).Select(goal => goal.UserId).ToListAsync());
            userIds.UnionWith(await _context.UserSubscriptions.Where(subscription => pageUserIds.Contains(subscription.UserId)).Select(subscription => subscription.UserId).ToListAsync());
            return userIds;
        }

        private async Task<bool> HasPreservedActivityAsync(Guid userId)
        {
            return await _context.Bookings.AnyAsync(booking => booking.UserId == userId || booking.CounselorId == userId)
                   || await _context.BookingParticipants.AnyAsync(participant => participant.UserId == userId)
                   || await _context.Counselors.AnyAsync(counselor => counselor.UserId == userId)
                   || await _context.CounselorProfiles.AnyAsync(profile => profile.UserId == userId)
                   || await _context.Feedback.AnyAsync(feedback => feedback.UserId == userId)
                   || await _context.ReflectionNotes.AnyAsync(note => note.UserId == userId)
                   || await _context.SessionFeedbacks.AnyAsync(feedback => feedback.UserId == userId)
                   || await _context.SupportTickets.AnyAsync(ticket => ticket.UserId == userId)
                   || await _context.UserGoals.AnyAsync(goal => goal.UserId == userId)
                   || await _context.UserSubscriptions.AnyAsync(subscription => subscription.UserId == userId);
        }

        private async Task SendAccountSetupAsync(ApplicationUser user, IEnumerable<string> roles)
        {
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var setupUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { userId = user.Id, code = encodedCode },
                protocol: Request.Scheme)
                ?? throw new InvalidOperationException("Could not create the password setup URL.");

            await _notifications.SendAdministratorCreatedAccountAsync(
                user,
                setupUrl,
                $"{user.Id:N}.{encodedCode}",
                string.Join(", ", roles.OrderBy(role => role)));
        }

        private async Task SendEmailConfirmationAsync(ApplicationUser user)
        {
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var confirmationUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = user.Id, code = encodedCode },
                protocol: Request.Scheme)
                ?? throw new InvalidOperationException("Could not create the email confirmation URL.");

            await _notifications.SendAccountConfirmationAsync(user, confirmationUrl, $"{user.Id:N}.{encodedCode}");
        }

        private async Task SendPasswordResetAsync(ApplicationUser user)
        {
            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var resetUrl = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { userId = user.Id, code = encodedCode },
                protocol: Request.Scheme)
                ?? throw new InvalidOperationException("Could not create the password reset URL.");

            await _notifications.SendPasswordResetAsync(user, resetUrl, $"{user.Id:N}.{encodedCode}");
        }

        private Guid? GetCurrentUserId()
        {
            return Guid.TryParse(_userManager.GetUserId(User), out var userId) ? userId : null;
        }

        private void NormalizePaging()
        {
            if (!AllowedPageSizes.Contains(PageSize))
                PageSize = AllowedPageSizes[0];

            if (PageNumber < 1)
                PageNumber = 1;
        }

        private static string CreateOneTimePassword()
        {
            return $"Cc!9{Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))}a";
        }

        private static string? NormalizePhone(string? phone)
        {
            return string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        }

        private static bool IsValidEmail(string? email)
        {
            return !string.IsNullOrWhiteSpace(email) && new EmailAddressAttribute().IsValid(email.Trim());
        }

        private IActionResult Error(string message)
        {
            TempData["Error"] = message;
            return RedirectToPage();
        }

        private static string FormatErrors(IdentityResult result)
        {
            return string.Join(" ", result.Errors.Select(error => error.Description));
        }
    }
}
