using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

[Index("BookingId", Name = "IX_Payments_Booking")]
public partial class Payment
{
    [Key]
    public Guid PaymentId { get; set; }

    public Guid BookingId { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? Amount { get; set; }

    [StringLength(50)]
    public string? PaymentStatus { get; set; }

    [StringLength(100)]
    public string? PaymentGateway { get; set; }

    [StringLength(200)]
    public string? TransactionReference { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("BookingId")]
    
    public virtual Booking Booking { get; set; } = null!;
}
