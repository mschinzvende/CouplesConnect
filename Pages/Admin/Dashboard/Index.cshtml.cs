using CouplesConnect.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CouplesConnect.Pages.Admin.Dashboard
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
        public int ApprovedCounselors { get; set; }
        public int PendingCounselors { get; set; }
        public int TotalBookings { get; set; }
        public int LiveSessions { get; set; }
        public decimal TotalRevenue { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int BookingsThisMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }

        public List<RecentBookingViewModel> RecentBookings { get; set; } = new();

        public string ChartLabels { get; set; }
        public string ChartData { get; set; } = "[]";
        public string BookingStatusLabels { get; set; } = "[]";
        public string BookingStatusData { get; set; } = "[]";

        public class RecentBookingViewModel
        {
            public string ClientName { get; set; } = "";
            public string CounselorName { get; set; } = "";
            public string ServiceName { get; set; } = "";
            public string Status { get; set; } = "";
            public DateTime ScheduledStart { get; set; }
        }

        public async Task OnGetAsync()
        {

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var paidStatuses = new[] { "Paid", "Completed", "Success" };

            TotalUsers = await _context.Users.CountAsync();
            ApprovedCounselors = await _context.CounselorProfiles.CountAsync(profile => profile.IsApproved);
            PendingCounselors = await _context.CounselorProfiles.CountAsync(profile => !profile.IsApproved && !profile.IsRejected);
            TotalBookings = await _context.Bookings.CountAsync();
            LiveSessions = await _context.Sessions.CountAsync(session => session.IsStarted && !session.IsEnded);
            TotalRevenue = await _context.Payments
                .Where(payment => payment.Amount != null && payment.PaymentStatus != null && paidStatuses.Contains(payment.PaymentStatus))
                .SumAsync(payment => payment.Amount ?? 0m);
            NewUsersThisMonth = await _context.Users.CountAsync(user => user.CreatedAt >= monthStart);
            BookingsThisMonth = await _context.Bookings.CountAsync(booking => booking.CreatedAt >= monthStart);
            RevenueThisMonth = await _context.Payments
                .Where(payment => payment.Amount != null && payment.PaidAt >= monthStart && payment.PaymentStatus != null && paidStatuses.Contains(payment.PaymentStatus))
                .SumAsync(payment => payment.Amount ?? 0m);

            RecentBookings = await _context.Bookings
                .Include(booking => booking.User)
                .Include(booking => booking.Counselor)
                .Include(booking => booking.Service)
                .OrderByDescending(booking => booking.CreatedAt)
                .Take(6)
                .Select(booking => new RecentBookingViewModel
                {
                    ClientName = booking.User.FullName,
                    CounselorName = booking.Counselor.FullName,
                    ServiceName = booking.Service.Name,
                    Status = booking.BookingStatus,
                    ScheduledStart = booking.ScheduledStart
                })
                .ToListAsync();

            var months = Enumerable.Range(0, 6)
                .Select(offset => monthStart.AddMonths(offset - 5))
                .ToList();
            var monthlyUsers = await _context.Users
                .Where(user => user.CreatedAt >= months.First())
                .GroupBy(user => new { user.CreatedAt.Year, user.CreatedAt.Month })
                .Select(group => new { group.Key.Year, group.Key.Month, Count = group.Count() })
                .ToListAsync();
            var bookingStatuses = await _context.Bookings
                .GroupBy(booking => booking.BookingStatus)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync();

            ChartLabels = JsonSerializer.Serialize(months.Select(month => month.ToString("MMM")));
            ChartData = JsonSerializer.Serialize(months.Select(month => monthlyUsers.FirstOrDefault(item => item.Year == month.Year && item.Month == month.Month)?.Count ?? 0));
            BookingStatusLabels = JsonSerializer.Serialize(bookingStatuses.Select(item => item.Status));
            BookingStatusData = JsonSerializer.Serialize(bookingStatuses.Select(item => item.Count));

        }

    }
}
