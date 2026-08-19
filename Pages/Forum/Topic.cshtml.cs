using System.ComponentModel.DataAnnotations;
using CouplesConnect.Data;
using CouplesConnect.Enums;
using CouplesConnect.Models;
using CouplesConnect.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Forum;

[Authorize]
public class TopicModel : PageModel
{
    private const int ReplyPageSize = 10;
    private static readonly TimeSpan ReplyCooldown = TimeSpan.FromSeconds(10);
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public TopicModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        INotificationService notificationService)
    {
        _context = context;
        _userManager = userManager;
        _notificationService = notificationService;
    }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty]
    public ReplyInput Input { get; set; } = new();

    public ForumTopic Topic { get; private set; } = null!;
    public List<ForumReply> Replies { get; private set; } = new();
    public int TotalReplies { get; private set; }
    public int TotalPages { get; private set; }
    public Guid CurrentUserId { get; private set; }
    public bool IsCurrentUserAdmin { get; private set; }

    public bool CanEditTopic => CurrentUserId == Topic.AuthorId || IsCurrentUserAdmin;
    public bool CanReply => !Topic.IsLocked && !Topic.IsArchived;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        return await LoadTopicAsync(id) ? Page() : NotFound();
    }

    public async Task<IActionResult> OnPostReplyAsync(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var topic = await _context.ForumTopics
            .FirstOrDefaultAsync(item => item.ForumTopicId == id && !item.IsDeleted && item.Category.IsActive);
        if (topic is null)
        {
            return NotFound();
        }

        if (topic.IsLocked || topic.IsArchived)
        {
            TempData["Error"] = "This discussion is closed to new replies.";
            return RedirectToPage(new { id });
        }

        if (!ModelState.IsValid)
        {
            await LoadTopicAsync(id);
            return Page();
        }

        var now = DateTime.UtcNow;
        var recentlyReplied = await _context.ForumReplies.AnyAsync(reply =>
            reply.AuthorId == user.Id && !reply.IsDeleted && reply.CreatedAt > now - ReplyCooldown);
        if (recentlyReplied)
        {
            ModelState.AddModelError(nameof(Input.Body), "Please wait a few seconds before posting another reply.");
            await LoadTopicAsync(id);
            return Page();
        }

        var reply = new ForumReply
        {
            ForumTopicId = topic.ForumTopicId,
            AuthorId = user.Id,
            Body = Input.Body.Trim(),
            IsAnonymous = Input.IsAnonymous,
            CreatedAt = now
        };

        topic.ReplyCount += 1;
        topic.LastActivityAt = now;
        _context.ForumReplies.Add(reply);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "This discussion changed while you were replying. Please review it and try again.";
            return RedirectToPage(new { id });
        }

        if (topic.AuthorId != user.Id)
        {
            try
            {
                var authorName = Input.IsAnonymous ? "A community member" : DisplayName(user);
                await _notificationService.NotifyAsync(
                    topic.AuthorId,
                    NotificationType.ForumReply,
                    "New reply to your discussion",
                    $"{authorName} replied to \"{topic.Title}\".",
                    $"/Forum/Topic/{topic.ForumTopicId}");
            }
            catch
            {
                // An in-app notification must not make a successfully saved reply fail.
            }
        }

        var replyPage = Math.Max(1, (int)Math.Ceiling(topic.ReplyCount / (double)ReplyPageSize));
        TempData["Success"] = "Your reply has been posted.";
        return RedirectToPage(
            "/Forum/Topic",
            pageHandler: null,
            routeValues: new { id, pageNumber = replyPage },
            fragment: $"reply-{reply.ForumReplyId}");
    }

    public async Task<IActionResult> OnPostDeleteReplyAsync(Guid id, Guid replyId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var reply = await _context.ForumReplies
            .Include(item => item.Topic)
            .FirstOrDefaultAsync(item => item.ForumReplyId == replyId && item.ForumTopicId == id && !item.IsDeleted);
        if (reply is null)
        {
            return NotFound();
        }

        if (reply.AuthorId != user.Id && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        reply.IsDeleted = true;
        reply.DeletedAt = DateTime.UtcNow;
        reply.Topic.ReplyCount = Math.Max(0, reply.Topic.ReplyCount - 1);
        var latestReply = await _context.ForumReplies
            .Where(item => item.ForumTopicId == id && !item.IsDeleted && item.ForumReplyId != replyId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => (DateTime?)item.CreatedAt)
            .FirstOrDefaultAsync();
        reply.Topic.LastActivityAt = latestReply ?? reply.Topic.CreatedAt;

        await _context.SaveChangesAsync();
        TempData["Success"] = "The reply has been removed.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteTopicAsync(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var topic = await _context.ForumTopics.FirstOrDefaultAsync(item => item.ForumTopicId == id && !item.IsDeleted);
        if (topic is null)
        {
            return NotFound();
        }

        if (topic.AuthorId != user.Id && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        topic.IsDeleted = true;
        topic.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "The discussion has been removed.";
        return RedirectToPage("/Forum/Index");
    }

    public async Task<IActionResult> OnPostModerateAsync(Guid id, string action)
    {
        if (!User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var topic = await _context.ForumTopics.FirstOrDefaultAsync(item => item.ForumTopicId == id && !item.IsDeleted);
        if (topic is null)
        {
            return NotFound();
        }

        switch (action)
        {
            case "toggle-lock":
                topic.IsLocked = !topic.IsLocked;
                TempData["Success"] = topic.IsLocked ? "Discussion locked." : "Discussion reopened.";
                break;
            case "toggle-pin":
                topic.IsPinned = !topic.IsPinned;
                TempData["Success"] = topic.IsPinned ? "Discussion pinned." : "Discussion unpinned.";
                break;
            case "toggle-archive":
                topic.IsArchived = !topic.IsArchived;
                TempData["Success"] = topic.IsArchived ? "Discussion archived." : "Discussion restored.";
                break;
            default:
                return BadRequest();
        }

        await _context.SaveChangesAsync();
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadTopicAsync(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        CurrentUserId = user?.Id ?? Guid.Empty;
        IsCurrentUserAdmin = User.IsInRole("Admin");

        Topic = await _context.ForumTopics
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Author)
            .FirstOrDefaultAsync(item => item.ForumTopicId == id && !item.IsDeleted && item.Category.IsActive) ?? null!;
        if (Topic is null)
        {
            return false;
        }

        var replies = _context.ForumReplies
            .AsNoTracking()
            .Include(item => item.Author)
            .Where(item => item.ForumTopicId == id && !item.IsDeleted)
            .OrderBy(item => item.CreatedAt);

        TotalReplies = await replies.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalReplies / (double)ReplyPageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);
        Replies = await replies
            .Skip((PageNumber - 1) * ReplyPageSize)
            .Take(ReplyPageSize)
            .ToListAsync();

        return true;
    }

    public static string DisplayName(ApplicationUser? user) =>
        string.IsNullOrWhiteSpace(user?.Alias)
            ? user?.FullName ?? "Community member"
            : user.Alias;
}

public class ReplyInput
{
    [Required, StringLength(5000, MinimumLength = 2)]
    [Display(Name = "Your reply")]
    public string Body { get; set; } = string.Empty;

    [Display(Name = "Post as Community member")]
    public bool IsAnonymous { get; set; }
}
