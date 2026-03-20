using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

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

   
}
