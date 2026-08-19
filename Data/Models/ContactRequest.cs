using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Data.Models;

/// <summary>
/// A public enquiry submitted from the unauthenticated Couples Connect landing page.
/// Contact requests are retained separately from authenticated support tickets.
/// </summary>
public class ContactRequest
{
    [Key]
    public Guid ContactRequestId { get; set; } = Guid.NewGuid();

    [Required, StringLength(120)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(3000)]
    public string Message { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Status { get; set; } = "New";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }
}
