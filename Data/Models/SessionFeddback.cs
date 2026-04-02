using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models
{
    public class SessionFeedback
    {
        [Key]
        public int SessionFeedbackId { get; set; }

        public int SessionId { get; set; }
        public Session Session { get; set; }

        public Guid UserId { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}