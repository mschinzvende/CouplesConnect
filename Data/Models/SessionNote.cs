using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models
{
    public class SessionNote
    {
        [Key]
        public int SessionNoteId { get; set; }

        public int SessionId { get; set; }
        public Session Session { get; set; }

        public Guid CounselorId { get; set; }

        public string Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}