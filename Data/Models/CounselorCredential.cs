using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

public partial class CounselorCredential
{
    [Key]
    public Guid CredentialId { get; set; }

    public Guid CounselorId { get; set; }

    [StringLength(150)]
    public string? CredentialType { get; set; }

    [StringLength(500)]
    public string? DocumentUrl { get; set; }

    public DateTime? UploadedAt { get; set; }

    [ForeignKey("CounselorId")]
    
    public virtual Counselor Counselor { get; set; } = null!;
}
