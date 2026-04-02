using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.Packages
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Package> Packages { get; set; }

        public async Task OnGetAsync()
        {
            Packages = await _context.Packages
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync(string Name, int SessionsPerMonth, decimal Price, string Description)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Name))
                {
                    TempData["Error"] = "Name is required";
                    return RedirectToPage();
                }

                if (string.IsNullOrWhiteSpace(Description))
                {
                    Description = "N/A";
                }


                var package = new Package
                {
                    Name = Name,
                    SessionsPerMonth = SessionsPerMonth,
                    Price = Price,
                    Description = Description
                };

                _context.Packages.Add(package);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Package created successfully";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error creating package";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditAsync(int PackageId, string Name, int SessionsPerMonth, decimal Price, string Description)
        {
            try
            {
                var package = await _context.Packages.FindAsync(PackageId);

                if (package == null)
                {
                    TempData["Error"] = "Package not found";
                    return RedirectToPage();
                }

                package.Name = Name;
                package.SessionsPerMonth = SessionsPerMonth;
                package.Price = Price;
                package.Description = Description;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Package updated successfully";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error updating package";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var package = await _context.Packages.FindAsync(id);

                if (package == null)
                {
                    TempData["Error"] = "Package not found";
                    return RedirectToPage();
                }

                _context.Packages.Remove(package);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Package deleted successfully";
            }
            catch (Exception)
            {
                TempData["Error"] = "Error deleting package";
            }

            return RedirectToPage();
        }
    }
}