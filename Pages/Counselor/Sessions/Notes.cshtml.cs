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
    public class NotesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotesModel(ApplicationDbContext context,
                          UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string ExistingNotes { get; set; }

        public async Task OnGetAsync(int sessionId)
        {
            var user = await _userManager.GetUserAsync(User);

            var note = await _context.SessionNotes
                .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.CounselorId == user.Id);

            ExistingNotes = note?.Notes;
        }

        public async Task<IActionResult> OnPostAsync(int sessionId, string Notes)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var existing = await _context.SessionNotes
                    .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.CounselorId == user.Id);

                if (existing == null)
                {
                    var note = new SessionNote
                    {
                        SessionId = sessionId,
                        CounselorId = user.Id,
                        Notes = Notes
                    };

                    _context.SessionNotes.Add(note);
                }
                else
                {
                    existing.Notes = Notes;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "Notes saved";
            }
            catch
            {
                TempData["Error"] = "Error saving notes";
            }

            return RedirectToPage(new { sessionId });
        }
    }
}