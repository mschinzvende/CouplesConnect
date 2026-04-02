using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
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

        public IndexModel(ApplicationDbContext context,
                          UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Booking> Bookings { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                Bookings = await _context.Bookings
                    .Include(b => b.Service)
                    .Include(b => b.Counselor)
                    .Include(b => b.Session) // ?? critical
                    .Where(b => b.UserId == user.Id)
                    .OrderByDescending(b => b.ScheduledStart)
                    .ToListAsync();
            }
            catch (Exception)
            {
                TempData["Error"] = "Error loading bookings";
            }
        }

        public async Task<IActionResult> OnPostCancelAsync(Guid id)
        {
            try
            {
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
                booking.CancelledAt = DateTime.UtcNow;

                // ALSO UPDATE SESSION
                if (booking.Session != null)
                {
                    booking.Session.Status = "Cancelled";
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Booking cancelled successfully";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error cancelling booking";
            }

            return RedirectToPage();
        }


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