using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

[Index("BookingId", Name = "IX_Sessions_Booking")]
public partial class Session
{
    [Key]
    public Guid SessionId { get; set; }

    public Guid BookingId { get; set; }

    [StringLength(500)]
    public string? MeetingLink { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public int? DurationMinutes { get; set; }

    [StringLength(50)]
    public string? SessionStatus { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("BookingId")]
    [InverseProperty("Sessions")]
    public virtual Booking Booking { get; set; } = null!;

    [InverseProperty("Session")]
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    [InverseProperty("Session")]
    public virtual ICollection<SessionNote> SessionNotes { get; set; } = new List<SessionNote>();
}
