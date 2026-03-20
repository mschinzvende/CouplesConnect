using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CouplesConnect.Pages.Account
{
    public class RegisterCounselorModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RegisterCounselorModel(ApplicationDbContext context,
                                      UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> OnPostAsync(
            string FullName,
            string Bio,
            string Qualifications,
            int YearsOfExperience,
            string Specializations)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                if (user == null)
                {
                    TempData["Error"] = "You must be logged in";
                    return RedirectToPage();
                }

                var profile = new CounselorProfile
                {
                    UserId = user.Id,
                    FullName = FullName,
                    Bio = Bio,
                    Qualifications = Qualifications,
                    YearsOfExperience = YearsOfExperience,
                    Specializations = Specializations
                };

                _context.CounselorProfiles.Add(profile);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Application submitted. Await approval.";
            }
            catch
            {
                TempData["Error"] = "Error submitting application";
            }

            return RedirectToPage();
        }
    }
}