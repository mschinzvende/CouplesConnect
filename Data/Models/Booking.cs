using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CouplesConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

[Index("CounselorId", Name = "IX_Bookings_Counselor")]
[Index("UserId", Name = "IX_Bookings_User")]
public partial class Booking
{
    [Key]
    public Guid BookingId { get; set; }

    public Guid UserId { get; set; }

    public Guid CounselorId { get; set; }

    public Guid ServiceId { get; set; }

    [StringLength(50)]
    public string? BookingStatus { get; set; }

    public bool? IsTrial { get; set; }

    public DateTime? ScheduledStart { get; set; }

    public DateTime? ScheduledEnd { get; set; }

    [StringLength(50)]
    public string? LocationMode { get; set; }

    [StringLength(150)]
    public string? CoreTopic { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("CounselorId")]
    [InverseProperty("Bookings")]
    public virtual Counselor Counselor { get; set; } = null!;

    [InverseProperty("Booking")]
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    [InverseProperty("Booking")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ForeignKey("ServiceId")]
    [InverseProperty("Bookings")]
    public virtual Service Service { get; set; } = null!;

    [InverseProperty("Booking")]
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();

    [ForeignKey("UserId")]
    public ApplicationUser User { get; set; }
}
