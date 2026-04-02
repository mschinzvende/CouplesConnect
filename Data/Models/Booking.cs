using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CouplesConnect.Models;
using Microsoft.EntityFrameworkCore;

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
    public UserSubscription Subscription { get; set; }

    public DateTime ScheduledStart { get; set; }

    public string BookingStatus { get; set; } = "Scheduled";

        public DateTime? CancelledAt { get; set; }
        public string CancelReason { get; set; }

        public Session Session { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
}