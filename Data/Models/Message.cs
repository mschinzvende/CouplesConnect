using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

[Index("BookingId", Name = "IX_Messages_Booking")]
public partial class Message
{
    [Key]
    public Guid MessageId { get; set; }

    public Guid BookingId { get; set; }

    public Guid SenderId { get; set; }

    public string? MessageText { get; set; }

    public bool? IsAnonymous { get; set; }

    public DateTime? SentAt { get; set; }

    [ForeignKey("BookingId")]
    [InverseProperty("Messages")]
    public virtual Booking Booking { get; set; } = null!;

    [ForeignKey("SenderId")]
    [InverseProperty("Messages")]
    public virtual User Sender { get; set; } = null!;
}
