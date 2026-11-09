using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Pages;

[RequestSizeLimit(16 * 1024)]
[ValidateAntiForgeryToken]
[EnableRateLimiting("public-contact")]
public class IndexModel : PageModel
{
    private static readonly HashSet<string> AllowedSubjects = new(StringComparer.Ordinal)
    {
        "Getting started",
        "Booking or rescheduling",
        "Counselling services",
        "Blind Reflection Room",
        "Privacy question",
        "Something else"
    };

    private readonly ApplicationDbContext _context;
    private readonly ILogger<IndexModel> _logger;
    private readonly IDataProtector _contactFormProtector;

    public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger,
        IDataProtectionProvider dataProtectionProvider)
    {
        _context = context;
        _logger = logger;
        _contactFormProtector = dataProtectionProvider.CreateProtector("CouplesConnect.PublicContactForm.v1");
    }

    [BindProperty]
    public ContactInput Contact { get; set; } = new();

    public void OnGet()
    {
        IssueContactFormToken();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        // Give bots a success-shaped response without storing their content or revealing the trap.
        if (!string.IsNullOrWhiteSpace(Contact.Website) || !IsPlausibleSubmissionTime(Contact.FormToken))
        {
            return SilentSuccess();
        }

        NormalizeContactInput();
        ValidateContactContent();

        if (!ModelState.IsValid)
        {
            IssueContactFormToken();
            return Page();
        }

        var duplicateCutoff = DateTime.UtcNow.AddMinutes(-30);
        var isDuplicate = await _context.ContactRequests.AsNoTracking().AnyAsync(x =>
            x.Email == Contact.Email && x.Message == Contact.Message && x.CreatedAt >= duplicateCutoff,
            cancellationToken);
        if (isDuplicate) return SilentSuccess();

        var enquiry = new ContactRequest
        {
            FullName = Contact.FullName,
            Email = Contact.Email,
            Subject = Contact.Subject,
            Message = Contact.Message,
            CreatedAt = DateTime.UtcNow
        };

        _context.ContactRequests.Add(enquiry);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(exception, "Unable to save a public contact enquiry.");
            ModelState.AddModelError(string.Empty, "We could not send your message just now. Please try again or email support@couplesconnect.com.");
            IssueContactFormToken();
            return Page();
        }

        TempData["ContactSuccess"] = "Thank you — your message is with our support team.";
        return Redirect("/#contact");
    }

    private void NormalizeContactInput()
    {
        Contact.FullName = NormalizeSingleLine(Contact.FullName ?? string.Empty);
        Contact.Email = NormalizeSingleLine(Contact.Email ?? string.Empty).ToLowerInvariant();
        Contact.Subject = NormalizeSingleLine(Contact.Subject ?? string.Empty);
        Contact.Message = NormalizeMessage(Contact.Message ?? string.Empty);
    }

    private void ValidateContactContent()
    {
        if (Contact.FullName.Length is < 2 or > 120)
            ModelState.AddModelError("Contact.FullName", "Please enter a name between 2 and 120 characters.");
        if (!AllowedSubjects.Contains(Contact.Subject))
            ModelState.AddModelError("Contact.Subject", "Please choose one of the available topics.");
        if (Contact.Message.Length is < 10 or > 3000)
            ModelState.AddModelError("Contact.Message", "Please enter between 10 and 3,000 characters.");

        var linkCount = Regex.Matches(Contact.Message, @"(?:https?://|www\.)", RegexOptions.IgnoreCase).Count;
        if (linkCount > 2)
            ModelState.AddModelError("Contact.Message", "Please include no more than two links.");
        if (Regex.IsMatch(Contact.Message, @"<\s*(?:script|iframe|object|embed|form)\b", RegexOptions.IgnoreCase))
            ModelState.AddModelError("Contact.Message", "HTML and embedded content are not accepted.");
    }

    private bool IsPlausibleSubmissionTime(string? token)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 512) return false;
        try
        {
            var issuedAtText = _contactFormProtector.Unprotect(token);
            if (!long.TryParse(issuedAtText, out var issuedAtSeconds)) return false;
            var elapsed = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(issuedAtSeconds);
            return elapsed >= TimeSpan.FromSeconds(2) && elapsed <= TimeSpan.FromHours(2);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    private void IssueContactFormToken()
    {
        Contact.FormToken = _contactFormProtector.Protect(DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
    }

    private IActionResult SilentSuccess()
    {
        TempData["ContactSuccess"] = "Thanks — your message has been received.";
        return Redirect("/#contact");
    }

    private static string NormalizeSingleLine(string value) =>
        RemoveUnsafeControlCharacters(value.Normalize(NormalizationForm.FormKC), allowLineBreaks: false).Trim();

    private static string NormalizeMessage(string value)
    {
        var normalized = RemoveUnsafeControlCharacters(
            value.Normalize(NormalizationForm.FormKC).Replace("\r\n", "\n").Replace('\r', '\n'),
            allowLineBreaks: true);
        return Regex.Replace(normalized, @"\n{4,}", "\n\n\n").Trim();
    }

    private static string RemoveUnsafeControlCharacters(string value, bool allowLineBreaks)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (!char.IsControl(character) || (allowLineBreaks && character is '\n' or '\t'))
                builder.Append(character);
        }
        return builder.ToString();
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
    [StringLength(200)]
    public string? Website { get; set; }

    [Required]
    [StringLength(512)]
    public string FormToken { get; set; } = string.Empty;
}
