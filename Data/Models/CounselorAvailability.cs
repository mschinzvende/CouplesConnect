using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models
{
    public class CounselorAvailability
    {
        [Key]
        public int AvailabilityId { get; set; }

        [Required]
        public Guid CounselorId { get; set; }

        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public bool IsActive { get; set; } = true;
    }
}