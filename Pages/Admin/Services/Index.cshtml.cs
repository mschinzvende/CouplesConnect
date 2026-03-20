using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.Services
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Service> Services { get; set; }
        public List<ServiceType> ServiceTypes { get; set; }

        public async Task OnGetAsync()
        {
            Services = await _context.Services
                .Include(s => s.ServiceType)
                .OrderBy(x => x.Name)
                .ToListAsync();

            ServiceTypes = await _context.ServiceTypes
                .Where(x => x.IsActive)
                .ToListAsync();
        }

        // CREATE

        public async Task<IActionResult> OnPostCreateAsync(string Name, int ServiceTypeId, string Description)
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


            var service = new Service
            {
                Name = Name,
                ServiceTypeId = ServiceTypeId,
                Description = Description
            };

            _context.Services.Add(service);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Service created successfully";
            return RedirectToPage();
        }

        // EDIT

        public async Task<IActionResult> OnPostEditAsync(int ServiceId, string Name, int ServiceTypeId, string Description)
        {
            var service = await _context.Services.FindAsync(ServiceId);

            if (service == null)
            {
                TempData["Error"] = "Service not found";
                return RedirectToPage();
            }

            service.Name = Name;
            service.ServiceTypeId = ServiceTypeId;
            service.Description = Description;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Service updated successfully";
            return RedirectToPage();
        }

        // DELETE

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var service = await _context.Services.FindAsync(id);

            if (service == null)
            {
                TempData["Error"] = "Service not found";
                return RedirectToPage();
            }

            _context.Services.Remove(service);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Service deleted successfully";
            return RedirectToPage();
        }
    }
}