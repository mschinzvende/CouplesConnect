using CouplesConnect.Models;
using System.ComponentModel.DataAnnotations;

namespace CouplesConnect.Data.Models
{
    public class BookingParticipant
    {
        [Key]
        public int Id { get; set; }

        public Guid BookingId { get; set; }
        public Booking Booking { get; set; }

        public string PhoneNumber { get; set; }

        public Guid? UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string Status { get; set; } = "Pending";

        public string InviteToken { get; set; }
    }
}