using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CouplesConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

public partial class UserSubscription
{
    [Key]
    public Guid SubscriptionId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(100)]
    public string? PlanName { get; set; }

    public DateTime? TrialStart { get; set; }

    public DateTime? TrialEnd { get; set; }

    public int? FreeSessionsRemaining { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    
    public ApplicationUser User { get; set; }
}
