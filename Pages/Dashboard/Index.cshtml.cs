using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Dashboard
{
    [Authorize]
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
        public int TotalBookings { get; set; }
        public int TrialRemaining { get; set; }
        public int OpenInquiries { get; set; }

        public List<Booking> UpcomingSessionsList { get; set; } = new();


        public async Task OnGetAsync()
        {

            var user = await _userManager.GetUserAsync(User);

            TotalBookings = await _context.Bookings
                .Where(x => x.UserId == user.Id)
                .CountAsync();

            UpcomingSessionsList = await _context.Bookings
                .Include(x => x.Service)
                .Include(x => x.Counselor)
                .ThenInclude(x => x.User)
                .Where(x => x.UserId == user.Id && x.BookingStatus == "Scheduled")
                .OrderBy(x => x.ScheduledStart)
                .Take(5)
                .ToListAsync();

            UpcomingSessions = UpcomingSessionsList.Count;

            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(x => x.UserId == user.Id && x.IsActive);

            TrialRemaining = subscription?.FreeSessionsRemaining ?? 0;

            OpenInquiries = 0;

        }

    }
}