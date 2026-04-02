using CouplesConnect.Data;
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
        public string ExistingComment { get; set; }

        public async Task OnGetAsync(int sessionId)
        {
            var user = await _userManager.GetUserAsync(User);

            var feedback = await _context.SessionFeedbacks
                .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.UserId == user.Id);

            if (feedback != null)
            {
                ExistingRating = feedback.Rating;
                ExistingComment = feedback.Comment;
            }
        }

        public async Task<IActionResult> OnPostAsync(int sessionId, int Rating, string Comment)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var existing = await _context.SessionFeedbacks
                    .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.UserId == user.Id);

                if (existing != null)
                {
                    existing.Rating = Rating;
                    existing.Comment = Comment;
                }
                else
                {
                    var feedback = new SessionFeedback
                    {
                        SessionId = sessionId,
                        UserId = user.Id,
                        Rating = Rating,
                        Comment = Comment
                    };

                    _context.SessionFeedbacks.Add(feedback);
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Feedback saved successfully";
            }
            catch
            {
                TempData["Error"] = "Error saving feedback";
            }

            return Redirect("/User/Bookings");
        }
    }
}