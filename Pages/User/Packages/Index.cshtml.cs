using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.User.Packages
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

        public List<Package> Packages { get; set; }

        public UserSubscription CurrentSubscription { get; set; }

        public async Task OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            Packages = await _context.Packages
                .Where(x => x.IsActive)
                .ToListAsync();

            CurrentSubscription = await _context.UserSubscriptions
                .Include(x => x.Package)
                .Where(x => x.UserId == user.Id && x.IsActive)
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefaultAsync();
        }

        public async Task<IActionResult> OnPostPurchaseAsync(int PackageId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                var package = await _context.Packages
                    .FirstOrDefaultAsync(x => x.PackageId == PackageId);

                if (package == null)
                {
                    TempData["Error"] = "Package not found";
                    return RedirectToPage();
                }

                // deactivate old subscription
                var existing = await _context.UserSubscriptions
                    .Where(x => x.UserId == user.Id && x.IsActive)
                    .ToListAsync();

                foreach (var sub in existing)
                {
                    sub.IsActive = false;
                }

                // create new subscription
                var newSub = new UserSubscription
                {
                    UserId = user.Id,
                    PackageId = package.PackageId,
                    SessionsRemaining = package.SessionsPerMonth,
                    IsTrial = false,
                    IsActive = true,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddMonths(1)
                };

                _context.UserSubscriptions.Add(newSub);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Package activated successfully";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error purchasing package";
            }

            return RedirectToPage();
        }
    }
}