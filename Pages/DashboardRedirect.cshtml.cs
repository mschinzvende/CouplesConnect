using CouplesConnect.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CouplesConnect.Pages
{
    public class DashboardRedirectModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardRedirectModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Redirect("/signin");

            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return Redirect("/Admin/Dashboard");

            if (await _userManager.IsInRoleAsync(user, "Counselor"))
                return Redirect("/User/Dashboard");

            if (await _userManager.IsInRoleAsync(user, "User"))
                return Redirect("/User/Dashboard");

            return Redirect("/User/Dashboard");
        }
    }
}