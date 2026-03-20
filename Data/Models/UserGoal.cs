using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CouplesConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

public partial class UserGoal
{
    [Key]
    public Guid GoalId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(200)]
    public string? GoalTitle { get; set; }

    public bool? IsAchieved { get; set; }

    public DateTime? CreatedAt { get; set; }

   
}
