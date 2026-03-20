using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Counselor.Availability
{
    [Authorize(Roles = "Counselor")]
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

        public List<CounselorAvailability> Availability { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            Availability = await _context.CounselorAvailabilities
                .Where(x => x.CounselorId == user.Id)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync(DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var slot = new CounselorAvailability
                {
                    CounselorId = user.Id,
                    DayOfWeek = DayOfWeek,
                    StartTime = StartTime,
                    EndTime = EndTime
                };

                _context.CounselorAvailabilities.Add(slot);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Availability added";
            }
            catch
            {
                TempData["Error"] = "Error saving availability";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var slot = await _context.CounselorAvailabilities.FindAsync(id);

                _context.CounselorAvailabilities.Remove(slot);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Removed";
            }
            catch
            {
                TempData["Error"] = "Error deleting";
            }

            return RedirectToPage();
        }
    }
}