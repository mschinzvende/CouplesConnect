using CouplesConnect.Data.Models;
using Microsoft.AspNetCore.Identity;

namespace CouplesConnect.Models
    {
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FullName { get; set; }

        public string? Alias { get; set; }

        public string? RelationshipStage { get; set; }

        public string? PrivacyLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Booking> Bookings { get; set; }
    }
}