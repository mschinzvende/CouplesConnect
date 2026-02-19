using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Models;

[Table("Feedback")]
public partial class Feedback
{
    [Key]
    public Guid FeedbackId { get; set; }

    public Guid SessionId { get; set; }

    public Guid UserId { get; set; }

    public int? Rating { get; set; }

    public string? Comments { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("SessionId")]
    [InverseProperty("Feedbacks")]
    public virtual Session Session { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("Feedbacks")]
    public virtual User User { get; set; } = null!;
}
