using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models
{
    public class ResourceCategory
    {
        [Key]
        public Guid CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(50)]
        public string Icon { get; set; }

        [StringLength(30)]
        public string Color { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public virtual ICollection<Resource> Resources { get; set; }
            = new List<Resource>();
    }
}