using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Models;

public partial class UserGoal
{
    [Key]
    public Guid GoalId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(200)]
    public string? GoalTitle { get; set; }

    public bool? IsAchieved { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserGoals")]
    public virtual User User { get; set; } = null!;
}
