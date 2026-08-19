using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Forum;

[Authorize]
public class IndexModel : PageModel
{
    private const int PageSize = 12;
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context) => _context = context;

    [BindProperty(SupportsGet = true)]
    public Guid? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "latest";

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public List<ForumCategorySummary> Categories { get; private set; } = new();
    public List<ForumTopic> Topics { get; private set; } = new();
    public int TotalTopics { get; private set; }
    public int TotalPages { get; private set; }

    public async Task OnGetAsync()
    {
        var visibleTopics = _context.ForumTopics
            .AsNoTracking()
            .Where(topic => !topic.IsDeleted && !topic.IsArchived && topic.Category.IsActive);

        var categoryActivity = await visibleTopics
            .GroupBy(topic => topic.ForumCategoryId)
            .Select(group => new
            {
                CategoryId = group.Key,
                TopicCount = group.Count(),
                LastActivityAt = group.Max(topic => topic.LastActivityAt)
            })
            .ToDictionaryAsync(item => item.CategoryId);

        var categories = await _context.ForumCategories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync();

        Categories = categories.Select(category =>
        {
            categoryActivity.TryGetValue(category.ForumCategoryId, out var activity);
            return new ForumCategorySummary
            {
                Category = category,
                TopicCount = activity?.TopicCount ?? 0,
                LastActivityAt = activity?.LastActivityAt
            };
        }).ToList();

        IQueryable<ForumTopic> query = visibleTopics
            .Include(topic => topic.Category)
            .Include(topic => topic.Author);

        if (CategoryId.HasValue && categories.All(category => category.ForumCategoryId != CategoryId.Value))
        {
            CategoryId = null;
        }

        if (CategoryId.HasValue)
        {
            query = query.Where(topic => topic.ForumCategoryId == CategoryId.Value);
        }

        var searchTerm = Search?.Trim();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.Length > 120 ? searchTerm[..120] : searchTerm;
            query = query.Where(topic => topic.Title.Contains(searchTerm) || topic.Body.Contains(searchTerm));
        }

        query = Sort.ToLowerInvariant() switch
        {
            "popular" => query.OrderByDescending(topic => topic.IsPinned)
                .ThenByDescending(topic => topic.ReplyCount)
                .ThenByDescending(topic => topic.LastActivityAt),
            "unanswered" => query.Where(topic => topic.ReplyCount == 0)
                .OrderByDescending(topic => topic.IsPinned)
                .ThenByDescending(topic => topic.CreatedAt),
            _ => query.OrderByDescending(topic => topic.IsPinned)
                .ThenByDescending(topic => topic.LastActivityAt)
        };

        Sort = Sort.ToLowerInvariant() is "popular" or "unanswered" ? Sort.ToLowerInvariant() : "latest";
        TotalTopics = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalTopics / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        Topics = await query
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}

public class ForumCategorySummary
{
    public ForumCategory Category { get; init; } = null!;
    public int TopicCount { get; init; }
    public DateTime? LastActivityAt { get; init; }
}
