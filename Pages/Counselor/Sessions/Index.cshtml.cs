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

        public List<Session> Sessions { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            Sessions = await _context.Sessions
                .Include(x => x.Booking)
                .ThenInclude(b => b.User)
                .Where(x => x.Booking.CounselorId == user.Id)
                .ToListAsync();
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

                session.Status = "Completed";
                session.EndedAt = DateTime.UtcNow;

                // CALCULATE EARNINGS

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
    }
}