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
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SubscriptionService _subscriptionService;

        public IndexModel(ApplicationDbContext context,
                          UserManager<ApplicationUser> userManager,
                          SubscriptionService subscriptionService)
        {
            _context = context;
            _userManager = userManager;
            _subscriptionService = subscriptionService;
        }

        public List<Booking> Bookings { get; set; } = new();

        // FILTERS
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; } = "all";

        [BindProperty(SupportsGet = true)]
        public string Search { get; set; }

        // PAGINATION
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

                var query = _context.Bookings
                    .Include(b => b.Service)
                    .Include(b => b.Counselor)
                    .Include(b => b.Session)
                    .Where(b => b.UserId == user.Id)
                    .AsQueryable();

                // ?? SEARCH
                if (!string.IsNullOrWhiteSpace(Search))
                {
                    query = query.Where(b =>
                        b.Service.Name.Contains(Search) ||
                        b.Counselor.FullName.Contains(Search));
                }

                // ?? FILTER (EF-TRANSLATABLE)
                if (StatusFilter == "Scheduled")
                {
                    query = query.Where(b =>
                        b.BookingStatus == "Scheduled" &&
                        b.ScheduledStart > now.AddMinutes(10));
                }
                else if (StatusFilter == "Ready")
                {
                    query = query.Where(b =>
                        b.BookingStatus == "Scheduled" &&
                        b.ScheduledStart <= now.AddMinutes(10) &&
                        b.ScheduledStart > now);
                }
                else if (StatusFilter == "Live")
                {
                    query = query.Where(b =>
                        b.BookingStatus == "Scheduled" &&
                        b.ScheduledStart <= now &&
                        b.ScheduledStart >= now.AddMinutes(-60));
                }
                else if (StatusFilter == "Completed")
                {
                    query = query.Where(b => b.BookingStatus == "Completed");
                }
                else if (StatusFilter == "Cancelled")
                {
                    query = query.Where(b => b.BookingStatus == "Cancelled");
                }

                // ?? ORDERING (BEST UX)
                query = query
                    .OrderBy(b => b.BookingStatus == "Cancelled") // push cancelled down
                    .ThenBy(b => b.ScheduledStart < now) // upcoming first
                    .ThenBy(b => b.ScheduledStart); // nearest first

                var totalCount = await query.CountAsync();

                TotalPages = (int)Math.Ceiling(totalCount / (double)PageSize);

                Bookings = await query
                    .Skip((PageNumber - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();
            }
            catch (Exception)
            {
                TempData["Error"] = "Error loading bookings";
            }
        }

        // ? CANCEL BOOKING
        public async Task<IActionResult> OnPostCancelAsync(Guid id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var booking = await _context.Bookings
                    .Include(b => b.Session)
                    .FirstOrDefaultAsync(b => b.BookingId == id);

                if (booking == null)
                {
                    TempData["Error"] = "Booking not found";
                    return RedirectToPage();
                }

                if (booking.BookingStatus != "Scheduled")
                {
                    TempData["Error"] = "Only scheduled bookings can be cancelled";
                    return RedirectToPage();
                }

                booking.BookingStatus = "Cancelled";
                booking.CancelledAt = DateTime.Now;

                // ?? SYNC SESSION
                if (booking.Session != null)
                {
                    booking.Session.Status = "Cancelled";
                }

                await _context.SaveChangesAsync();

                await _subscriptionService.RestoreSession(user.Id, booking.SubscriptionId);

                TempData["Success"] = "Booking cancelled successfully";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error cancelling booking";
            }

            return RedirectToPage();
        }

        // ?? UI STATE LOGIC (SAFE TO KEEP HERE)
        public string GetSessionState(Booking b)
        {
            var now = DateTime.UtcNow;

            if (b.BookingStatus == "Cancelled")
                return "Cancelled";

            if (b.BookingStatus == "Completed")
                return "Completed";

            var diff = b.ScheduledStart - now;

            if (diff.TotalMinutes <= 0 && diff.TotalMinutes >= -60)
                return "Live";

            if (diff.TotalMinutes <= 10)
                return "Ready";

            return "Scheduled";
        }
    }
}