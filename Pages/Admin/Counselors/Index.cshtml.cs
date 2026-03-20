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