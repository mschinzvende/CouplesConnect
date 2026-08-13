using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.User.Sessions
{
    [Authorize(Roles = "User")]
    public class FeedbackModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FeedbackModel(ApplicationDbContext context,
                             UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public int ExistingRating { get; set; }
        public string ExistingComment { get; set; } = string.Empty;
        public string CounselorName { get; set; } = string.Empty;

        public async Task<IActionResult> OnGetAsync(int sessionId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Challenge();

            var booking = await GetBookingForUserAsync(sessionId, user.Id);
            if (booking is null)
                return Forbid();

            CounselorName = booking.Counselor.FullName;

            var feedback = await _context.SessionFeedbacks
                .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.UserId == user.Id);

            if (feedback != null)
            {
                ExistingRating = feedback.Rating;
                ExistingComment = feedback.Comment;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int sessionId, int rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
                return Challenge();

            var booking = await GetBookingForUserAsync(sessionId, user.Id);
            if (booking is null)
                return Forbid();

            if (rating is < 1 or > 5)
            {
                TempData["Error"] = "Please select a rating between 1 and 5 stars.";
                return RedirectToPage(new { sessionId });
            }

            try
            {
                var existing = await _context.SessionFeedbacks
                    .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.UserId == user.Id);

                if (existing != null)
                {
                    existing.Rating = rating;
                    existing.Comment = comment?.Trim() ?? string.Empty;
                }
                else
                {
                    var feedback = new SessionFeedback
                    {
                        SessionId = sessionId,
                        UserId = user.Id,
                        Rating = rating,
                        Comment = comment?.Trim() ?? string.Empty
                    };

                    _context.SessionFeedbacks.Add(feedback);
                }

                await _context.SaveChangesAsync();
                await UpdateCounselorRatingAsync(booking.CounselorId);

                TempData["Success"] = "Feedback saved successfully";
            }
            catch
            {
                TempData["Error"] = "Error saving feedback";
                return RedirectToPage(new { sessionId });
            }

            return Redirect("/User/Bookings");
        }

        private Task<Booking?> GetBookingForUserAsync(int sessionId, Guid userId)
        {
            return _context.Bookings
                .Include(booking => booking.Counselor)
                .FirstOrDefaultAsync(booking =>
                    booking.SessionId == sessionId &&
                    (booking.UserId == userId ||
                     booking.Participants.Any(participant =>
                         participant.UserId == userId && participant.Status == "Accepted")));
        }

        private async Task UpdateCounselorRatingAsync(Guid counselorId)
        {
            var counselor = await _context.Counselors
                .FirstOrDefaultAsync(item => item.UserId == counselorId);

            if (counselor is null)
                return;

            counselor.Rating = await _context.SessionFeedbacks
                .Where(feedback => _context.Bookings.Any(booking =>
                    booking.SessionId == feedback.SessionId && booking.CounselorId == counselorId))
                .AverageAsync(feedback => (decimal?)feedback.Rating);

            await _context.SaveChangesAsync();
        }
    }
}
