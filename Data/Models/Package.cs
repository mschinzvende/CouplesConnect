using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models
{
    public class Package
    {
        public int PackageId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public int SessionsPerMonth { get; set; }

        public decimal Price { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}