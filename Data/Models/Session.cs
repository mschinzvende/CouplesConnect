using CouplesConnect.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models
{
    public class Session
    {
        [Key]
        public int SessionId { get; set; }

        public Guid BookingId { get; set; }
        public Booking Booking { get; set; }

        public string MeetingLink { get; set; }

        public string Status { get; set; } = "Scheduled";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}