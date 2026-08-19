using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CouplesConnect.Models;

/// <summary>
/// A chronological reply in a forum topic. Replies are soft-deleted to retain
/// accountable moderation without exposing removed content.
/// </summary>
public class ForumReply
{
    [Key]
    public Guid ForumReplyId { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ForumTopicId { get; set; }

    [ForeignKey(nameof(ForumTopicId))]
    public ForumTopic Topic { get; set; } = null!;

    [Required]
    public Guid AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public ApplicationUser Author { get; set; } = null!;

    [Required, StringLength(5000)]
    public string Body { get; set; } = string.Empty;

    public bool IsAnonymous { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
