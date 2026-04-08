using CouplesConnect.Models;
using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Data.Models
{
    public class Booking
    {
        [Key]
        public Guid BookingId { get; set; }

        public Guid UserId { get; set; }
        public ApplicationUser User { get; set; }

        public Guid CounselorId { get; set; }
        public ApplicationUser Counselor { get; set; }

        public int ServiceId { get; set; }
        public Service Service { get; set; }

        public int? SubscriptionId { get; set; }

        public DateTime ScheduledStart { get; set; }

        public string BookingStatus { get; set; } = "Scheduled";

        public bool IsGroup { get; set; } = false;

        public int SessionId { get; set; }
        public Session Session { get; set; }

        public ICollection<BookingParticipant> Participants { get; set; } = new List<BookingParticipant>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}