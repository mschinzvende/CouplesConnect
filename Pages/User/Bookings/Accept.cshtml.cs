using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CouplesConnect.Services.Interfaces;


namespace CouplesConnect.Pages.User.Bookings
{
    [Authorize]
    public class AcceptModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        private readonly INotificationService _notification;

        public AcceptModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager, INotificationService notification)
        {
            _context = context;
            _userManager = userManager;
            _notification = notification;
        }

        public async Task<IActionResult> OnGetAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return NotFound();

            var participant = await _context.BookingParticipants
                .FirstOrDefaultAsync(x => x.InviteToken == token);

            if (participant == null)
                return NotFound();

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Challenge(); // Redirects to login
            }

            participant.UserId = user.Id;
            participant.Status = "Accepted";

            await _context.SaveChangesAsync();

            await _notification.NotifyInvitationAcceptedAsync(
    participant.Id);

            return RedirectToPage("/User/Bookings/Index");
        }
    }
}