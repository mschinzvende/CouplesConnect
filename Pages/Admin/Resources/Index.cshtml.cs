using CouplesConnect.Data;
using CouplesConnect.Models;
using CouplesConnect.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.Resources
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public IndexModel(ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public List<Resource> Resources { get; set; } = new();

        [BindProperty]
        public ResourceViewModel Input { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            Resources = await _context.Resources
                .Include(x => x.Category)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            Input.Categories = await _context.ResourceCategories
                .Where(x => x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.CategoryId.ToString()
                })
                .ToListAsync();
        }


        public async Task<IActionResult> OnPostSaveAsync()
        {
            await LoadData();

            if (!ModelState.IsValid)
                return Page();

            string thumbnail = Input.ExistingThumbnail ?? "";

            if (Input.ThumbnailFile != null)
            {
                var folder = Path.Combine(_environment.WebRootPath, "images", "resources");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(Input.ThumbnailFile.FileName);

                var path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);

                await Input.ThumbnailFile.CopyToAsync(stream);

                thumbnail = "/images/resources/" + fileName;
            }

            var resource = new Resource
            {
                ResourceId = Guid.NewGuid(),
                Title = Input.Title,
                Description = Input.Description,
                CategoryId = Input.CategoryId,
                HtmlPage = Input.HtmlPage,
                EstimatedDuration = Input.EstimatedDuration,
                DisplayOrder = Input.DisplayOrder,
                IsFeatured = Input.IsFeatured,
                IsActive = Input.IsActive,
                Thumbnail = thumbnail
            };

            _context.Resources.Add(resource);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Resource created successfully.";

            return RedirectToPage();
        }
    }
}