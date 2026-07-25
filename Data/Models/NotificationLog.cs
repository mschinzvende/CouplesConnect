using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Models
{
    public class NotificationLog
    {
        public int Id { get; set; }

        [MaxLength(50)]
        public string Channel { get; set; } = "";

        [MaxLength(50)]
        public string NotificationType { get; set; } = "";

        public Guid? UserId { get; set; }

        public Guid? BookingId { get; set; }

        [MaxLength(200)]
        public string Recipient { get; set; } = "";

        [MaxLength(200)]
        public string Subject { get; set; } = "";

        public string Message { get; set; } = "";

        [MaxLength(30)]
        public string Status { get; set; } = "Pending";

        public string? ProviderResponse { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? SentDate { get; set; }
    }
}