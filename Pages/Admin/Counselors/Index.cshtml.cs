using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using CouplesConnect.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.Counselors
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly INotificationService _notifications;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context,
                          UserManager<ApplicationUser> userManager,
                           INotificationService notifications,
                           ILogger<IndexModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _notifications = notifications;
            _logger = logger;
        }

        public List<CounselorProfile> Counselors { get; set; }

        public async Task OnGetAsync()
        {
            Counselors = await _context.CounselorProfiles
                .Include(x => x.User)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostRejectAsync(Guid id, string reason)
        {
            try
            {
                var profile = await _context.CounselorProfiles
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.UserId == id);

                if (profile == null)
                {
                    TempData["Error"] = "Not found";
                    return RedirectToPage();
                }

                reason = string.IsNullOrWhiteSpace(reason)
                    ? "The application did not meet the current approval requirements."
                    : reason.Trim();

                profile.IsApproved = false;
                profile.IsRejected = true;
                profile.RejectionReason = reason;

                await _context.SaveChangesAsync();

                try
                {
                    await _notifications.NotifyCounselorApplicationRejectedAsync(profile.UserId, reason);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to notify counselor {CounselorUserId} of application rejection", profile.UserId);
                }

                TempData["Success"] = "Application rejected and the applicant has been notified.";
            }
            catch
            {
                TempData["Error"] = "Error rejecting";
            }

            return RedirectToPage();
        }
        public async Task<PartialViewResult> OnGetDetailsAsync(Guid id)
        {
            var profile = await _context.CounselorProfiles
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserId == id);

            var files = await _context.CounselorCredentials
                .Where(x => x.CounselorId == id)
                .ToListAsync();

            ViewData["Files"] = files;


            var services = (from s in _context.Services
                            join cs in _context.CounselorServices
                                on s.ServiceId equals cs.ServiceId
                            where cs.CounselorId == id
                            select s).ToList();

            ViewData["Services"] = services;

            return new PartialViewResult
            {
                ViewName = "_CounselorDetails",
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<CounselorProfile>(ViewData, profile)
            };
        }

        public async Task<IActionResult> OnPostApproveAsync(Guid id)
        {
            try
            {
                var profile = await _context.CounselorProfiles
                    .Include(x => x.User)
                    .FirstOrDefaultAsync(x => x.UserId == id);

                if (profile == null)
                {
                    TempData["Error"] = "Not found";
                    return RedirectToPage();
                }

                profile.IsApproved = true;
                profile.IsRejected = false;
                profile.RejectionReason = null;

                if (!await _userManager.IsInRoleAsync(profile.User, "Counselor"))
                {
                    await _userManager.AddToRoleAsync(profile.User, "Counselor");
                }

                await _context.SaveChangesAsync();

                try
                {
                    await _notifications.NotifyCounselorApplicationApprovedAsync(profile.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unable to notify counselor {CounselorUserId} of application approval", profile.UserId);
                }

                TempData["Success"] = "Counselor approved and the applicant has been notified.";
            }
            catch
            {
                TempData["Error"] = "Error approving counselor";
            }

            return RedirectToPage();
        }
    }
}
