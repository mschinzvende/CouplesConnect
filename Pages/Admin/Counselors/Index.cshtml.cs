using CouplesConnect.Data;
using CouplesConnect.Models;
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

        public IndexModel(ApplicationDbContext context,
                          UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                    .FirstOrDefaultAsync(x => x.UserId == id);

                if (profile == null)
                {
                    TempData["Error"] = "Not found";
                    return RedirectToPage();
                }

                profile.IsApproved= false;
                //profile.RejectionReason = reason;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Application rejected";
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

                await _userManager.AddToRoleAsync(profile.User, "Counselor");

                await _context.SaveChangesAsync();

                TempData["Success"] = "Counselor approved";
            }
            catch
            {
                TempData["Error"] = "Error approving counselor";
            }

            return RedirectToPage();
        }
    }
}