using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CouplesConnect.Models
{
    public class Service
    {
        [Key]
        public int ServiceId { get; set; }

        [Required]
        public int ServiceTypeId { get; set; }

        public ServiceType ServiceType { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}