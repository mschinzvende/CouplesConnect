using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Models;

public partial class Resource
{
    [Key]
    public Guid ResourceId { get; set; }

    [StringLength(200)]
    public string? Title { get; set; }

    public string? Content { get; set; }

    [StringLength(50)]
    public string? ResourceType { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}
