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

                var validSlots = new List<(DateTime Start, DateTime End)>();
                foreach (var s in slots)
                {
                    if (string.IsNullOrEmpty(s?.start) || string.IsNullOrEmpty(s?.end))
                        throw new InvalidOperationException("One or more selected slots are invalid.");

                    if (!DateTimeOffset.TryParse(s.start, out var startOffset) ||
                        !DateTimeOffset.TryParse(s.end, out var endOffset))
                        throw new InvalidOperationException("One or more selected slots are invalid.");

                    var start = startOffset.LocalDateTime;
                    var end = endOffset.LocalDateTime;

                    if (start < DateTime.Now || end <= start || end - start != TimeSpan.FromHours(1))
                        throw new InvalidOperationException("Availability must be a future one-hour slot.");

                    if (validSlots.Any(existing => start < existing.End && end > existing.Start))
                        throw new InvalidOperationException("Selected availability slots overlap.");

                    var exists = await _context.CounselorAvailabilities
                        .AnyAsync(x =>
                            x.CounselorId == user.Id &&
                            x.StartDateTime < end &&
                            x.EndDateTime > start
                        );

                    if (exists) continue;

                    validSlots.Add((start, end));
                }

                foreach (var slot in validSlots)
                {
                    _context.CounselorAvailabilities.Add(new CounselorAvailability
                    {
                        CounselorId = user.Id,
                        StartDateTime = slot.Start,
                        EndDateTime = slot.End
                    });
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Availability added";
            }
            catch (Exception)
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
            var user = await _userManager.GetUserAsync(User);
            var slot = await _context.CounselorAvailabilities
                .FirstOrDefaultAsync(item => item.AvailabilityId == id && item.CounselorId == user.Id);

            if (slot != null)
            {
                _context.CounselorAvailabilities.Remove(slot);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }
    }
}
