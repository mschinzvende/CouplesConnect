using CouplesConnect.Data;
using CouplesConnect.Data.Models;
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
                    .Include(s => s.Bookings)
                        .ThenInclude(b => b.User)
                    .Include(s => s.Bookings)
                        .ThenInclude(b => b.Participants)
                            .ThenInclude(p => p.User)
                    .Where(s => s.Bookings.Any(b => b.CounselorId == user.Id))
                    .AsQueryable();

                // SEARCH (by any participant)
                if (!string.IsNullOrWhiteSpace(Search))
                {
                    query = query.Where(s =>
                        s.Bookings.Any(b =>
                            b.User.FullName.Contains(Search) ||
                            b.Participants.Any(p => p.User != null && p.User.FullName.Contains(Search))
                        ));
                }

                // FILTER
                if (StatusFilter == "Upcoming")
                {
                    query = query.Where(s =>
                        !s.IsEnded &&
                        s.Bookings.Any(b => b.ScheduledStart > now.AddMinutes(10)));
                }
                else if (StatusFilter == "Ready")
                {
                    query = query.Where(s =>
                        !s.IsEnded &&
                        s.Bookings.Any(b =>
                            b.ScheduledStart <= now.AddMinutes(10) &&
                            b.ScheduledStart > now));
                }
                else if (StatusFilter == "Live")
                {
                    query = query.Where(s =>
                        s.IsStarted && !s.IsEnded);
                }
                else if (StatusFilter == "Completed")
                {
                    query = query.Where(s => s.IsEnded);
                }

                query = query
                    .OrderBy(s => s.IsEnded)
                    .ThenBy(s => s.Bookings.Min(b => b.ScheduledStart));

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

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCompleteAsync(int id)
        {
            var session = await _context.Sessions
                .Include(s => s.Bookings)
                .FirstOrDefaultAsync(s => s.SessionId == id);

            if (session == null)
            {
                TempData["Error"] = "Session not found";
                return RedirectToPage();
            }

            session.IsEnded = true;
            session.EndedAt = DateTime.UtcNow;

            // mark all bookings completed
            foreach (var b in session.Bookings)
            {
                b.BookingStatus = "Completed";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Session completed";

            return RedirectToPage();
        }

        public string GetSessionState(Session s)
        {
            var now = DateTime.UtcNow;

            if (s.IsEnded) return "Completed";
            if (s.IsStarted) return "Live";

            var nextTime = s.Bookings.Min(b => b.ScheduledStart);
            var diff = nextTime - now;

            if (diff.TotalMinutes <= 10) return "Ready";

            return "Upcoming";
        }
    }
}