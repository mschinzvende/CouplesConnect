using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Sessions
{
    [Authorize]
    public class RoomModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;


        public RoomModel(ApplicationDbContext context , UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int SessionId { get; set; }
        public bool IsCounselor { get; set; }
        public string ExistingNotes { get; set; }

        public List<Booking> UpcomingSessions { get; set; } = new();
        public DateTime? NextSessionTime { get; set; }

        public DateTime SessionStartTime { get; set; }
        public async Task<IActionResult> OnGetAsync(int sessionId)
        {
            SessionId = sessionId;
            

            var user = await _userManager.GetUserAsync(User);

            // ?? Get booking tied to session
            var booking = await _context.Bookings
                .Include(b => b.Session)
                .FirstOrDefaultAsync(b => b.Session.SessionId == sessionId);

            SessionStartTime = booking.ScheduledStart;

            if (booking == null)
                return NotFound();

            var isOwner = booking.UserId == user.Id;
            var isCounselor = booking.CounselorId == user.Id;

            // ?? Access control
            if (!isOwner && !isCounselor)
                return Forbid();

            IsCounselor = isCounselor;

            // ?? Apply 5-min rule ONLY to users
            if (!isCounselor)
            {
                var minutesToStart = (booking.ScheduledStart - DateTime.Now).TotalMinutes;

                /*Temporary Deactivate constraint
                if (minutesToStart > 5)
                {
                    TempData["Error"] = "Session can only be joined 5 minutes before start time.";
                    return Redirect("/User/Dashboard");
                }*/
            }

            // ? Counselor logic (unchanged)
            if (IsCounselor)
            {
                var note = await _context.SessionNotes
                    .FirstOrDefaultAsync(x => x.SessionId == sessionId);

                ExistingNotes = note?.Notes;

                UpcomingSessions = await _context.Bookings
                    .Include(x => x.User)
                    .Include(x => x.Session)
                    .Where(x => x.CounselorId == user.Id &&
                                x.BookingStatus == "Scheduled" &&
                                x.ScheduledStart > DateTime.UtcNow)
                    .OrderBy(x => x.ScheduledStart)
                    .Take(5)
                    .ToListAsync();

                NextSessionTime = UpcomingSessions.FirstOrDefault()?.ScheduledStart;
            }

            return Page();
        }


        public async Task<IActionResult> OnPostSaveNotesAsync(SaveNotesDto dto)
        {
            try
            {
                if (dto == null || dto.SessionId == 0)
                {
                    return new JsonResult(new { success = false });
                }

                var user = await _userManager.GetUserAsync(User);
                var userId = user.Id;

                var existing = await _context.SessionNotes
                    .FirstOrDefaultAsync(x => x.SessionId == dto.SessionId);

                if (existing == null)
                {
                    _context.SessionNotes.Add(new SessionNote
                    {
                        SessionId = dto.SessionId,
                        CounselorId = userId,
                        Notes = dto.Notes ?? ""
                    });
                }
                else
                {
                    existing.Notes = dto.Notes ?? "";
                }

                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true });
            }
            catch (Exception ex)
            {
                // log this if needed
                return new JsonResult(new { success = false });
            }
        }

        public class SaveNotesDto
        {
            public int SessionId { get; set; }
            public string Notes { get; set; }
        }
    }
}