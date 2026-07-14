using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CouplesConnect.Models
{
    public class Resource
    {
        [Key]
        public Guid ResourceId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(200)]
        public string Slug { get; set; }      // NEW

        [StringLength(600)]
        public string Description { get; set; }

        public Guid CategoryId { get; set; }

        [ForeignKey(nameof(CategoryId))]
        public ResourceCategory Category { get; set; }

        public string Thumbnail { get; set; }

        // Example:
        // daily-check-in/index.html
        public string HtmlPage { get; set; }

        public string EstimatedDuration { get; set; }

      

       

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

       

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}