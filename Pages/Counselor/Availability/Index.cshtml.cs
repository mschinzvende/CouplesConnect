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

        [BindProperty]
        public DateTime SelectedDate { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            Availability = await _context.CounselorAvailabilities
                .Where(x => x.CounselorId == user.Id)
                .OrderBy(x => x.StartDateTime)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync(string SelectedSlots)
        {
            try
            {
                if (string.IsNullOrEmpty(SelectedSlots))
                {
                    TempData["Error"] = "No slots selected";
                    return RedirectToPage();
                }

                var user = await _userManager.GetUserAsync(User);

                var slots = System.Text.Json.JsonSerializer.Deserialize<List<SlotDto>>(SelectedSlots);

                if (slots == null || !slots.Any())
                {
                    TempData["Error"] = "Invalid slot data";
                    return RedirectToPage();
                }

                foreach (var s in slots)
                {
                    // ?? CRITICAL NULL CHECK
                    if (string.IsNullOrEmpty(s?.start) || string.IsNullOrEmpty(s?.end))
                        continue;

                    var start = DateTime.Parse(s.start);
                    var end = DateTime.Parse(s.end);

                    var exists = await _context.CounselorAvailabilities
                        .AnyAsync(x =>
                            x.CounselorId == user.Id &&
                            x.StartDateTime < end &&
                            x.EndDateTime > start
                        );

                    if (exists) continue;

                    _context.CounselorAvailabilities.Add(new CounselorAvailability
                    {
                        CounselorId = user.Id,
                        StartDateTime = start,
                        EndDateTime = end
                    });
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Availability added";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error saving availability";
            }

            return RedirectToPage();
        }

        public class SlotDto
        {
            public string start { get; set; }
            public string end { get; set; }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var slot = await _context.CounselorAvailabilities.FindAsync(id);

            if (slot != null)
            {
                _context.CounselorAvailabilities.Remove(slot);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}