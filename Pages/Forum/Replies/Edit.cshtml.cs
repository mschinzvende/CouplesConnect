using System.ComponentModel.DataAnnotations;
using CouplesConnect.Data;
using CouplesConnect.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Forum.Replies;

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
    public EditReplyInput Input { get; set; } = new();

    public Guid ReplyId { get; private set; }
    public Guid TopicId { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var reply = await GetEditableReplyAsync(id);
        if (reply is null)
        {
            return NotFound();
        }

        ReplyId = reply.ForumReplyId;
        TopicId = reply.ForumTopicId;
        Input = new EditReplyInput { Body = reply.Body, IsAnonymous = reply.IsAnonymous };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        var reply = await GetEditableReplyAsync(id);
        if (reply is null)
        {
            return NotFound();
        }

        ReplyId = reply.ForumReplyId;
        TopicId = reply.ForumTopicId;
        if (!ModelState.IsValid)
        {
            return Page();
        }

        reply.Body = Input.Body.Trim();
        reply.IsAnonymous = Input.IsAnonymous;
        reply.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            TempData["Error"] = "This reply changed while you were editing. Please try again.";
            return RedirectToPage(
                "/Forum/Topic",
                pageHandler: null,
                routeValues: new { id = reply.ForumTopicId },
                fragment: $"reply-{reply.ForumReplyId}");
        }

        TempData["Success"] = "Your reply has been updated.";
        return RedirectToPage(
            "/Forum/Topic",
            pageHandler: null,
            routeValues: new { id = reply.ForumTopicId },
            fragment: $"reply-{reply.ForumReplyId}");
    }

    private async Task<ForumReply?> GetEditableReplyAsync(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return null;
        }

        var reply = await _context.ForumReplies
            .Include(item => item.Topic)
            .FirstOrDefaultAsync(item => item.ForumReplyId == id && !item.IsDeleted && !item.Topic.IsDeleted);
        if (reply is null || (reply.AuthorId != user.Id && !User.IsInRole("Admin")))
        {
            return null;
        }

        if ((reply.Topic.IsLocked || reply.Topic.IsArchived) && !User.IsInRole("Admin"))
        {
            return null;
        }

        return reply;
    }
}

public class EditReplyInput
{
    [Required, StringLength(5000, MinimumLength = 2)]
    [Display(Name = "Your reply")]
    public string Body { get; set; } = string.Empty;

    [Display(Name = "Post as Community member")]
    public bool IsAnonymous { get; set; }
}
