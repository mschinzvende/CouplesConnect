using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace CouplesConnect.Pages.User.Bookings
{
    [Authorize]
    public class AcceptModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AcceptModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync(string token)
        {
            var user = await _userManager.GetUserAsync(User);

            var participant = await _context.BookingParticipants
                .FirstOrDefaultAsync(x => x.InviteToken == token);

            if (participant == null)
                return NotFound();

            participant.UserId = user.Id;
            participant.Status = "Accepted";

            await _context.SaveChangesAsync();

            return Redirect("/User/Bookings");
        }
    }
}