using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models;

/// <summary>
/// A curated area of discussion in the Couples Connect community.
/// Categories are managed by administrators and are never hard-deleted.
/// </summary>
public class ForumCategory
{
    [Key]
    public Guid ForumCategoryId { get; set; } = Guid.NewGuid();

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Slug { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Icon { get; set; } = "mdi mdi-forum-outline";

    [Required, StringLength(20)]
    public string Color { get; set; } = "#4f46e5";

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ForumTopic> Topics { get; set; } = new List<ForumTopic>();
}
