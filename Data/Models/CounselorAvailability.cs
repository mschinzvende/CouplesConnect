using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

[Table("CounselorAvailability")]
public partial class CounselorAvailability
{
    [Key]
    public Guid AvailabilityId { get; set; }

    public Guid CounselorId { get; set; }

    public int? DayOfWeek { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public bool? IsAvailable { get; set; }

    [ForeignKey("CounselorId")]
   
    public virtual Counselor Counselor { get; set; } = null!;
}
