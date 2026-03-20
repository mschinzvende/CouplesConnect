using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CouplesConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

public partial class Counselor
{
    [Key]
    public Guid CounselorId { get; set; }

    public Guid UserId { get; set; }

    public string? Bio { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal? HourlyRate { get; set; }

    [StringLength(50)]
    public string? VerificationStatus { get; set; }

    [Column(TypeName = "decimal(3, 2)")]
    public decimal? Rating { get; set; }

    public int? TotalSessions { get; set; }

    public DateTime? CreatedAt { get; set; }

}
