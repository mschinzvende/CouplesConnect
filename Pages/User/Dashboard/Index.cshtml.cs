using CouplesConnect.Data;
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
        public int TotalBookings { get; set; }
        public int CompletedSessions { get; set; }
        public int ResourcesViewed { get; set; }

        public string ChartLabels { get; set; }
        public string ChartData { get; set; }

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

            ResourcesViewed = 12;

            var months = new List<string> { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            var values = new List<int> { 2, 3, 1, 4, 2, 5 };

            ChartLabels = JsonSerializer.Serialize(months);
            ChartData = JsonSerializer.Serialize(values);

        }

    }
}