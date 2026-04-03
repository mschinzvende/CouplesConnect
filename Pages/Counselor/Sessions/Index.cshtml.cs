using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Counselor.Sessions
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

        public List<Session> Sessions { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "all";

        public int PageNumber { get; set; } = 1;
        public int TotalPages { get; set; }

        private const int PageSize = 6;

        public async Task OnGetAsync(int pageNumber = 1)
        {
            try
            {
                PageNumber = pageNumber;

                var user = await _userManager.GetUserAsync(User);
                var now = DateTime.UtcNow;

                var query = _context.Sessions
                    .Include(x => x.Booking)
                        .ThenInclude(b => b.User)
                    .Where(x => x.Booking.CounselorId == user.Id)
                    .AsQueryable();

                // ?? SEARCH
                if (!string.IsNullOrWhiteSpace(Search))
                {
                    query = query.Where(x =>
                        x.Booking.User.FullName.Contains(Search));
                }

                // ?? FILTER
                if (StatusFilter == "Upcoming")
                {
                    query = query.Where(x =>
                        x.Status != "Completed" &&
                        x.Status != "Cancelled" &&
                        x.Booking.ScheduledStart > now.AddMinutes(10));
                }
                else if (StatusFilter == "Ready")
                {
                    query = query.Where(x =>
                        x.Status != "Completed" &&
                        x.Status != "Cancelled" &&
                        x.Booking.ScheduledStart <= now.AddMinutes(10) &&
                        x.Booking.ScheduledStart > now);
                }
                else if (StatusFilter == "Live")
                {
                    query = query.Where(x =>
                        x.Status != "Completed" &&
                        x.Status != "Cancelled" &&
                        x.Booking.ScheduledStart <= now &&
                        x.Booking.ScheduledStart >= now.AddMinutes(-60));
                }
                else if (StatusFilter == "Completed")
                {
                    query = query.Where(x => x.Status == "Completed");
                }
                else if (StatusFilter == "Cancelled")
                {
                    query = query.Where(x =>
                        x.Status == "Cancelled" ||
                        x.Booking.BookingStatus == "Cancelled");
                }

                // ?? ORDERING
                query = query
                    .OrderBy(x => x.Status == "Cancelled")
                    .ThenBy(x => x.Status == "Completed")
                    .ThenBy(x => x.Booking.ScheduledStart < now)
                    .ThenBy(x => x.Booking.ScheduledStart);

                var totalCount = await query.CountAsync();

                TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

                Sessions = await query
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();
            }
            catch
            {
                TempData["Error"] = "Error loading sessions";
            }
        }

        public async Task<IActionResult> OnPostStartAsync(int id)
        {
            try
            {
                var session = await _context.Sessions.FindAsync(id);

                if (session == null)
                {
                    TempData["Error"] = "Session not found";
                    return RedirectToPage();
                }

                session.IsStarted = true;
                session.StartedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Session started";
            }
            catch
            {
                TempData["Error"] = "Error starting session";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteAsync(int id)
        {
            try
            {
                var session = await _context.Sessions
                    .Include(x => x.Booking)
                    .FirstOrDefaultAsync(x => x.SessionId == id);

                if (session == null)
                {
                    TempData["Error"] = "Session not found";
                    return RedirectToPage();
                }

                if (session.Status == "Completed")
                {
                    TempData["Error"] = "Session already completed";
                    return RedirectToPage();
                }

                session.Status = "Completed";
                session.EndedAt = DateTime.UtcNow;

                var settings = await _context.PlatformSettings.FirstOrDefaultAsync();

                var sub = await _context.UserSubscriptions
                    .FirstOrDefaultAsync(x => x.SubscriptionId == session.Booking.SubscriptionId);

                decimal sessionValue = 0;

                if (sub?.PackageId != null)
                {
                    var package = await _context.Packages.FindAsync(sub.PackageId);
                    sessionValue = package.Price / package.SessionsPerMonth;
                }

                var earning = new CounselorEarning
                {
                    CounselorId = session.Booking.CounselorId,
                    BookingId = session.BookingId,
                    Amount = sessionValue * settings.CounselorCommissionPercent / 100
                };

                _context.CounselorEarnings.Add(earning);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Session completed";
            }
            catch
            {
                TempData["Error"] = "Error completing session";
            }

            return RedirectToPage();
        }

        public string GetSessionState(Session s)
        {
            var now = DateTime.UtcNow;

            if (s.Status == "Completed") return "Completed";
            if (s.Status == "Cancelled" || s.Booking.BookingStatus == "Cancelled") return "Cancelled";

            var diff = s.Booking.ScheduledStart - now;

            if (diff.TotalMinutes <= 0 && diff.TotalMinutes >= -60) return "Live";
            if (diff.TotalMinutes <= 10) return "Ready";

            return "Upcoming";
        }
    }
}