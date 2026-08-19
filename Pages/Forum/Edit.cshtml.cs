using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Forum;

[Authorize]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public EditModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public TopicInput Input { get; set; } = new();

    public List<ForumCategory> Categories { get; private set; } = new();
    public Guid TopicId { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var topic = await GetEditableTopicAsync(id);
        if (topic is null)
        {
            return NotFound();
        }

        TopicId = id;
        Input = new TopicInput
        {
            ForumCategoryId = topic.ForumCategoryId,
            Title = topic.Title,
            Body = topic.Body,
            IsAnonymous = topic.IsAnonymous
        };
        await LoadCategoriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var topic = await GetEditableTopicAsync(id);
        if (topic is null)
        {
            return NotFound();
        }

        TopicId = id;
        await LoadCategoriesAsync();
        if (!Categories.Any(category => category.ForumCategoryId == Input.ForumCategoryId))
        {
            ModelState.AddModelError(nameof(Input.ForumCategoryId), "Choose an active discussion category.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        topic.ForumCategoryId = Input.ForumCategoryId;
        topic.Title = Input.Title.Trim();
        topic.Body = Input.Body.Trim();
        topic.IsAnonymous = Input.IsAnonymous;
        topic.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "This discussion was updated elsewhere. Please review the latest version.";
            return RedirectToPage(new { id });
        }

        TempData["Success"] = "Your discussion has been updated.";
        return RedirectToPage("/Forum/Topic", new { id });
    }

    private async Task<ForumTopic?> GetEditableTopicAsync(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return null;
        }

        var topic = await _context.ForumTopics
            .FirstOrDefaultAsync(item => item.ForumTopicId == id && !item.IsDeleted);
        if (topic is null || (topic.AuthorId != user.Id && !User.IsInRole("Admin")))
        {
            return null;
        }

        if (topic.IsArchived && !User.IsInRole("Admin"))
        {
            return null;
        }

        return topic;
    }

    private async Task LoadCategoriesAsync()
    {
        Categories = await _context.ForumCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync();
    }
}
