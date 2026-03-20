using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models
{
    public class CounselorAvailability
    {
        [Key]
        public int AvailabilityId { get; set; }

        [Required]
        public Guid CounselorId { get; set; }

        

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public bool IsActive { get; set; } = true;
    }
}