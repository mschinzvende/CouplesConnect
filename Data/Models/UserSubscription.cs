using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CouplesConnect.Models
{
    public class UserSubscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        public Guid UserId { get; set; }

        public int? PackageId { get; set; }

        public Package Package { get; set; }

        public int SessionsRemaining { get; set; }

        public bool IsTrial { get; set; }

        public bool IsActive { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }
    }
}