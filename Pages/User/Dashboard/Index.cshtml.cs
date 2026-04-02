using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CouplesConnect.Pages.User.Dashboard
{

    [Authorize(Roles = "User")]
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

        public int UpcomingSessions { get; set; }
        public int SessionsRemaining { get; set; }
        public int TotalBookings { get; set; }
        public int CompletedSessions { get; set; }
        public int ResourcesViewed { get; set; }

        public string ChartLabels { get; set; }
        public string ChartData { get; set; }

        public int CounselorSessions { get; set; }
        public decimal TotalEarnings { get; set; }
        public List<Booking> CounselorUpcoming { get; set; } = new();

        public async Task OnGetAsync()
        {

            var user = await _userManager.GetUserAsync(User);

            UpcomingSessions = await _context.Bookings
            .Where(x => x.UserId == user.Id && x.BookingStatus == "Scheduled")
            .CountAsync();

            TotalBookings = await _context.Bookings
            .Where(x => x.UserId == user.Id)
            .CountAsync();

            CompletedSessions = await _context.Bookings
            .Where(x => x.UserId == user.Id && x.BookingStatus == "Completed")
            .CountAsync();

            var sub = await _context.UserSubscriptions
            .FirstOrDefaultAsync(x => x.UserId == user.Id && x.IsActive);

            SessionsRemaining = sub?.SessionsRemaining ?? 0;

            if (SessionsRemaining == 0)
            {
                TempData["Error"] = "Your trial has ended. Please upgrade your plan.";
            }

            ResourcesViewed = 12;

            var months = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            var values = new List<int> { 2, 3, 1, 4, 2, 5 };

            ChartLabels = JsonSerializer.Serialize(months);
            ChartData = JsonSerializer.Serialize(values);

            if (User.IsInRole("Counselor"))
            {
                CounselorSessions = await _context.Bookings
                    .CountAsync(x => x.CounselorId == user.Id);

                TotalEarnings = await _context.CounselorEarnings
                    .Where(x => x.CounselorId == user.Id)
                    .SumAsync(x => x.Amount);

                CounselorUpcoming = await _context.Bookings
                    .Include(x => x.User)
                    .Include(x => x.Session)
                    .Where(x => x.CounselorId == user.Id &&
                                x.BookingStatus == "Scheduled")
                    .OrderBy(x => x.ScheduledStart)
                    .Take(5)
                    .ToListAsync();
            }

        }

    }
}