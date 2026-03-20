using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

public partial class SessionNote
{
    [Key]
    public Guid NoteId { get; set; }

    public Guid SessionId { get; set; }

    public Guid CounselorId { get; set; }

    public string? Notes { get; set; }

    public DateTime? CreatedAt { get; set; }
}
