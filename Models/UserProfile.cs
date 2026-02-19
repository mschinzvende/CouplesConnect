using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Models;

public partial class UserProfile
{
    [Key]
    public Guid ProfileId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(150)]
    public string? FullName { get; set; }

    [StringLength(100)]
    public string? Alias { get; set; }

    [StringLength(100)]
    public string? RelationshipStage { get; set; }

    [StringLength(50)]
    public string? PrivacyLevel { get; set; }

    public DateTime? CreatedAt { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("UserProfiles")]
    public virtual User User { get; set; } = null!;
}
