using System.Text.RegularExpressions;
using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.Forum;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context) => _context = context;

    [BindProperty]
    public CategoryInput Input { get; set; } = new();

    public List<ForumCategoryAdminItem> Categories { get; private set; } = new();

    public async Task OnGetAsync()
    {
        Categories = await _context.ForumCategories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .Select(category => new ForumCategoryAdminItem
            {
                Category = category,
                TopicCount = category.Topics.Count(topic => !topic.IsDeleted)
            })
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var error = await ValidateCategoryAsync(Input.Name, Input.Description, Input.Icon, Input.Color, null);
        if (error is not null)
        {
            TempData["Error"] = error;
            return RedirectToPage();
        }

        var category = new ForumCategory
        {
            Name = Input.Name.Trim(),
            Slug = await CreateUniqueSlugAsync(Input.Name),
            Description = Input.Description.Trim(),
            Icon = Input.Icon.Trim(),
            Color = Input.Color.Trim(),
            DisplayOrder = Input.DisplayOrder,
            IsActive = Input.IsActive
        };

        _context.ForumCategories.Add(category);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Forum category created.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostEditAsync(Guid id, string name, string description, string icon, string color, int displayOrder, bool isActive)
    {
        var category = await _context.ForumCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var error = await ValidateCategoryAsync(name, description, icon, color, id);
        if (error is not null)
        {
            TempData["Error"] = error;
            return RedirectToPage();
        }

        var nameChanged = !string.Equals(category.Name, name.Trim(), StringComparison.Ordinal);
        category.Name = name.Trim();
        category.Description = description.Trim();
        category.Icon = icon.Trim();
        category.Color = color.Trim();
        category.DisplayOrder = displayOrder;
        category.IsActive = isActive;
        if (nameChanged)
        {
            category.Slug = await CreateUniqueSlugAsync(category.Name, category.ForumCategoryId);
        }

        await _context.SaveChangesAsync();
        TempData["Success"] = "Forum category updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid id)
    {
        var category = await _context.ForumCategories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        category.IsActive = !category.IsActive;
        await _context.SaveChangesAsync();
        TempData["Success"] = category.IsActive ? "Category is now visible to the community." : "Category has been hidden from the community.";
        return RedirectToPage();
    }

    private async Task<string?> ValidateCategoryAsync(string? name, string? description, string? icon, string? color, Guid? excludingId)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < 2 or > 100)
        {
            return "Category names must be between 2 and 100 characters.";
        }

        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length is < 10 or > 500)
        {
            return "Category descriptions must be between 10 and 500 characters.";
        }

        if (string.IsNullOrWhiteSpace(icon) || icon.Trim().Length > 80)
        {
            return "Choose a valid icon class (up to 80 characters).";
        }

        if (string.IsNullOrWhiteSpace(color) || !Regex.IsMatch(color.Trim(), "^#[0-9a-fA-F]{6}$"))
        {
            return "Choose a valid category colour.";
        }

        var normalizedName = name.Trim().ToUpperInvariant();
        var exists = await _context.ForumCategories.AnyAsync(category =>
            category.Name.ToUpper() == normalizedName && (!excludingId.HasValue || category.ForumCategoryId != excludingId.Value));
        return exists ? "A category with this name already exists." : null;
    }

    private async Task<string> CreateUniqueSlugAsync(string name, Guid? excludingId = null)
    {
        var slug = Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        slug = string.IsNullOrWhiteSpace(slug) ? "community" : slug;
        var candidate = slug;
        var suffix = 2;

        while (await _context.ForumCategories.AnyAsync(category =>
                   category.Slug == candidate && (!excludingId.HasValue || category.ForumCategoryId != excludingId.Value)))
        {
            candidate = $"{slug}-{suffix++}";
        }

        return candidate;
    }
}

public class CategoryInput
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = "mdi mdi-forum-outline";
    public string Color { get; set; } = "#4f46e5";
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ForumCategoryAdminItem
{
    public ForumCategory Category { get; init; } = null!;
    public int TopicCount { get; init; }
}
