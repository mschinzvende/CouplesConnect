using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Resources
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Resource> FeaturedResources { get; set; } = new();

        public List<Resource> Resources { get; set; } = new();

        public List<ResourceCategory> Categories { get; set; } = new();

        public async Task OnGetAsync()
        {
            Categories = await _context.ResourceCategories
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            FeaturedResources = await _context.Resources
                .Include(x => x.Category)
                .Where(x => x.IsActive && x.IsFeatured)
                .OrderBy(x => x.DisplayOrder)
                .Take(3)
                .ToListAsync();

            Resources = await _context.Resources
                .Include(x => x.Category)
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();
        }
    }
}