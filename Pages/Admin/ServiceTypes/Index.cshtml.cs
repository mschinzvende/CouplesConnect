using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.ServiceTypes
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ServiceType> ServiceTypes { get; set; }

        public async Task OnGetAsync()
        {
            ServiceTypes = await _context.ServiceTypes
                .OrderBy(x => x.Name)
                .ToListAsync();
        }

        // CREATE

        public async Task<IActionResult> OnPostCreateAsync(string Name, string Description)
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

            var service = new ServiceType
            {
                Name = Name,
                Description = Description
            };

            _context.ServiceTypes.Add(service);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Service created successfully";

            return RedirectToPage();
        }

        // EDIT

        public async Task<IActionResult> OnPostEditAsync(int ServiceTypeId, string Name, string Description)
        {
            var service = await _context.ServiceTypes.FindAsync(ServiceTypeId);

            if (service == null)
            {
                TempData["Error"] = "Service not found";
                return RedirectToPage();
            }

            service.Name = Name;
            service.Description = Description;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Service updated successfully";

            return RedirectToPage();
        }

        // DELETE

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var service = await _context.ServiceTypes.FindAsync(id);

            if (service == null)
            {
                TempData["Error"] = "Service not found";
                return RedirectToPage();
            }

            _context.ServiceTypes.Remove(service);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Service deleted successfully";

            return RedirectToPage();
        }
    }
}