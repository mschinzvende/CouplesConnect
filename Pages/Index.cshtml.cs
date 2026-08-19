using System.ComponentModel.DataAnnotations;
using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    [BindProperty]
    public ContactInput Contact { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // This invisible field catches simple automated submissions without affecting real visitors.
        if (!string.IsNullOrWhiteSpace(Contact.Website))
        {
            TempData["ContactSuccess"] = "Thanks — your message has been received.";
            return Redirect("/#contact");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var enquiry = new ContactRequest
        {
            FullName = Contact.FullName.Trim(),
            Email = Contact.Email.Trim(),
            Subject = Contact.Subject.Trim(),
            Message = Contact.Message.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.ContactRequests.Add(enquiry);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Unable to save a public contact enquiry.");
            ModelState.AddModelError(string.Empty, "We could not send your message just now. Please try again or email support@couplesconnect.com.");
            return Page();
        }

        TempData["ContactSuccess"] = "Thank you — your message is with our support team.";
        return Redirect("/#contact");
    }
}

public class ContactInput
{
    [Required(ErrorMessage = "Please enter your name.")]
    [StringLength(120)]
    [Display(Name = "Your name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter an email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a topic.")]
    [StringLength(120)]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please tell us how we can help.")]
    [StringLength(3000, MinimumLength = 10, ErrorMessage = "Please enter between 10 and 3,000 characters.")]
    public string Message { get; set; } = string.Empty;

    [Range(typeof(bool), "true", "true", ErrorMessage = "Please confirm that you have read the Privacy Policy.")]
    public bool PrivacyAccepted { get; set; }

    // Honeypot: intentionally omitted from the visual form.
    public string? Website { get; set; }
}
