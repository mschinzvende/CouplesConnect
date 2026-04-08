using CouplesConnect.Data.Models;
using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models
{
    public class Session
    {
        [Key]
        public int SessionId { get; set; }

        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

        public string MeetingLink { get; set; }

        public string Status { get; set; } = "Scheduled";

        public bool IsStarted { get; set; } = false;
        public bool IsEnded { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }
}