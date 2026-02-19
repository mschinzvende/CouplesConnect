using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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

    [InverseProperty("Counselor")]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    [InverseProperty("Counselor")]
    public virtual ICollection<CounselorAvailability> CounselorAvailabilities { get; set; } = new List<CounselorAvailability>();

    [InverseProperty("Counselor")]
    public virtual ICollection<CounselorCredential> CounselorCredentials { get; set; } = new List<CounselorCredential>();

    [InverseProperty("Counselor")]
    public virtual ICollection<SessionNote> SessionNotes { get; set; } = new List<SessionNote>();

    [ForeignKey("UserId")]
    [InverseProperty("Counselors")]
    public virtual User User { get; set; } = null!;
}
