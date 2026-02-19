using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Models;

public partial class Service
{
    [Key]
    public Guid ServiceId { get; set; }

    [StringLength(150)]
    public string? Name { get; set; }

    public string? Description { get; set; }

    public int? DurationMinutes { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? BasePrice { get; set; }

    public bool? IsActive { get; set; }

    [InverseProperty("Service")]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
