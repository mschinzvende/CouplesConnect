using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using CouplesConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.User.Bookings
{
    [Authorize(Roles = "User")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SubscriptionService _subscriptionService;

        public CreateModel(ApplicationDbContext context,
                           UserManager<ApplicationUser> userManager,
                           SubscriptionService subscriptionService)
        {
            _context = context;
            _userManager = userManager;
            _subscriptionService = subscriptionService;
        }

        public List<Service> Services { get; set; }
        public List<ApplicationUser> Counselors { get; set; }

        public int? ServiceId { get; set; }
        public Guid CounselorId { get; set; }
        public DateTime? SelectedDate { get; set; }

        public List<DateTime> AvailableDates { get; set; } = new();
        public List<DateTime> TimeSlots { get; set; } = new();

        public async Task OnGetAsync(int? ServiceId, Guid CounselorId, DateTime? SelectedDate)
        {
            this.ServiceId = ServiceId;
            this.CounselorId = CounselorId;
            this.SelectedDate = SelectedDate;

            Services = await _context.Services.Where(x => x.IsActive).ToListAsync();

            Counselors = await _context.CounselorProfiles
    .Where(x => x.IsApproved)
    .Include(x => x.User)
    .Select(x => x.User)
    .ToListAsync();

            if (CounselorId!=null)
            {
                var availability = await _context.CounselorAvailabilities
                    .Where(x => x.CounselorId == CounselorId)
                    .ToListAsync();

                // Next 7 days
                for (int i = 0; i < 7; i++)
                {
                    var date = DateTime.Today.AddDays(i);

                    if (availability.Any(a => a.DayOfWeek == date.DayOfWeek))
                    {
                        AvailableDates.Add(date);
                    }
                }

                if (SelectedDate.HasValue)
                {
                    var daySlots = availability
                        .Where(a => a.DayOfWeek == SelectedDate.Value.DayOfWeek);

                    foreach (var slot in daySlots)
                    {
                        var start = SelectedDate.Value.Date.Add(slot.StartTime);
                        var end = SelectedDate.Value.Date.Add(slot.EndTime);

                        while (start < end)
                        {
                            TimeSlots.Add(start);
                            start = start.AddMinutes(60); // 1-hour slots
                        }
                    }

                    // Remove already booked slots
                    var booked = await _context.Bookings
                        .Where(b => b.CounselorId == CounselorId &&
                                    b.ScheduledStart.Date == SelectedDate.Value.Date)
                        .Select(b => b.ScheduledStart)
                        .ToListAsync();

                    TimeSlots = TimeSlots
                        .Where(t => !booked.Contains(t))
                        .ToList();
                }
            }
        }

        public async Task<IActionResult> OnPostAsync(int ServiceId, Guid CounselorId, DateTime ScheduledStart)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (!await _subscriptionService.HasSessions(user.Id))
                {
                    TempData["Error"] = "No sessions remaining";
                    return Redirect("/User/Packages");
                }

                var booking = new Booking
                {
                    UserId = user.Id,
                    CounselorId = CounselorId,
                    ServiceId = ServiceId,
                    ScheduledStart = ScheduledStart
                };

                _context.Bookings.Add(booking);

                await _subscriptionService.ConsumeSession(user.Id);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Session booked successfully";
            }
            catch
            {
                TempData["Error"] = "Booking failed";
            }

            return RedirectToPage();
        }
    }
}