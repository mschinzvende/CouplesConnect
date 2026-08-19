using System.ComponentModel.DataAnnotations;
using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Forum;

[Authorize]
public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [BindProperty]
    public TopicInput Input { get; set; } = new();

    public List<ForumCategory> Categories { get; private set; } = new();

    public async Task OnGetAsync(Guid? categoryId)
    {
        await LoadCategoriesAsync();
        if (categoryId.HasValue && Categories.Any(category => category.ForumCategoryId == categoryId.Value))
        {
            Input.ForumCategoryId = categoryId.Value;
        }
    }

    public async Task<IActionResult> OnPostAsync()
    {
        await LoadCategoriesAsync();

        if (!Categories.Any(category => category.ForumCategoryId == Input.ForumCategoryId))
        {
            ModelState.AddModelError(nameof(Input.ForumCategoryId), "Choose an active discussion category.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var now = DateTime.UtcNow;
        var topic = new ForumTopic
        {
            ForumCategoryId = Input.ForumCategoryId,
            AuthorId = user.Id,
            Title = Input.Title.Trim(),
            Body = Input.Body.Trim(),
            IsAnonymous = Input.IsAnonymous,
            CreatedAt = now,
            LastActivityAt = now
        };

        _context.ForumTopics.Add(topic);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Your discussion has been published.";
        return RedirectToPage("/Forum/Topic", new { id = topic.ForumTopicId });
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

public class TopicInput
{
    [Required(ErrorMessage = "Choose a category for this discussion.")]
    public Guid ForumCategoryId { get; set; }

    [Required, StringLength(160, MinimumLength = 6)]
    [Display(Name = "Discussion title")]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(8000, MinimumLength = 15)]
    [Display(Name = "What would you like to discuss?")]
    public string Body { get; set; } = string.Empty;

    [Display(Name = "Post as Community member")]
    public bool IsAnonymous { get; set; }
}
