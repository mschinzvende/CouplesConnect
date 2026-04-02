using CouplesConnect.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.Reports
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int TotalUsers { get; set; }
        public int TotalCounselors { get; set; }
        public int TotalBookings { get; set; }
        public int TotalSessions { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal TotalEarnings { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // USERS
                TotalUsers = await _context.Users.CountAsync();

                // COUNSELORS (approved only)
                TotalCounselors = await _context.CounselorProfiles
                    .CountAsync(x => x.IsApproved);

                // BOOKINGS
                TotalBookings = await _context.Bookings.CountAsync();

                // SESSIONS
                TotalSessions = await _context.Sessions.CountAsync();

                // REVENUE (from subscriptions/packages)
                TotalRevenue = await _context.UserSubscriptions
                    .Where(x => !x.IsTrial && x.PackageId != null)
                    .Join(_context.Packages,
                        sub => sub.PackageId,
                        pkg => pkg.PackageId,
                        (sub, pkg) => pkg.Price)
                    .SumAsync();

                // COUNSELOR EARNINGS
                TotalEarnings = await _context.CounselorEarnings
                    .SumAsync(x => x.Amount);
            }
            catch
            {
                TotalUsers = 0;
                TotalCounselors = 0;
                TotalBookings = 0;
                TotalSessions = 0;
                TotalRevenue = 0;
                TotalEarnings = 0;

                TempData["Error"] = "Error loading reports";
            }
        }
    }
}