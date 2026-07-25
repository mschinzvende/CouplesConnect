using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CouplesConnect.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        /// <summary>
        /// Booking, Reminder, Invitation, General etc.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Type { get; set; }

        public bool IsRead { get; set; } = false;

        public string? Link { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ReadDate { get; set; }
    }
}