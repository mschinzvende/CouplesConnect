using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages.Admin.ContactRequests;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private static readonly string[] ValidStatuses = ["New", "In progress", "Resolved"];
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context) => _context = context;

    public IReadOnlyList<ContactRequest> Requests { get; private set; } = [];
    public int OpenCount { get; private set; }
    public string? CurrentStatus { get; private set; }

    public async Task OnGetAsync(string? status)
    {
        CurrentStatus = ValidStatuses.Contains(status) ? status : null;
        var requestQuery = _context.ContactRequests.AsNoTracking();

        OpenCount = await requestQuery.CountAsync(request => request.Status != "Resolved");
        if (CurrentStatus is not null)
        {
            requestQuery = requestQuery.Where(request => request.Status == CurrentStatus);
        }

        Requests = await requestQuery
            .OrderBy(request => request.Status == "Resolved")
            .ThenByDescending(request => request.CreatedAt)
            .Take(200)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostSetStatusAsync(Guid id, string status, string? returnStatus)
    {
        if (!ValidStatuses.Contains(status))
        {
            TempData["Error"] = "That enquiry status is not valid.";
            return RedirectToPage(new { status = returnStatus });
        }

        var request = await _context.ContactRequests.FindAsync(id);
        if (request is null)
        {
            TempData["Error"] = "That contact enquiry could not be found.";
            return RedirectToPage(new { status = returnStatus });
        }

        request.Status = status;
        request.ResolvedAt = status == "Resolved" ? DateTime.UtcNow : null;
        await _context.SaveChangesAsync();

        TempData["Success"] = status == "Resolved"
            ? "Enquiry marked as resolved."
            : $"Enquiry marked as {status.ToLowerInvariant()}.";
        return RedirectToPage(new { status = returnStatus });
    }
}
