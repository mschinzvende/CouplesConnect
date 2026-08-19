using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CouplesConnect.Models;

/// <summary>
/// The opening post of a forum conversation. Content is soft-deleted so the
/// moderation and audit trail are preserved.
/// </summary>
public class ForumTopic
{
    [Key]
    public Guid ForumTopicId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ForumCategoryId { get; set; }

    [ForeignKey(nameof(ForumCategoryId))]
    public ForumCategory Category { get; set; } = null!;

    [Required]
    public Guid AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public ApplicationUser Author { get; set; } = null!;

    [Required, StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(8000)]
    public string Body { get; set; } = string.Empty;

    public bool IsAnonymous { get; set; }

    public bool IsPinned { get; set; }

    public bool IsLocked { get; set; }

    public bool IsArchived { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    /// <summary>Denormalized for fast category and search listings.</summary>
    public int ReplyCount { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<ForumReply> Replies { get; set; } = new List<ForumReply>();
}
