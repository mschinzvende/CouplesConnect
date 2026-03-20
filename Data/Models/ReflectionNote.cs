using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CouplesConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

public partial class ReflectionNote
{
    [Key]
    public Guid ReflectionId { get; set; }

    public Guid UserId { get; set; }

    public string? NoteText { get; set; }

    public DateTime? CreatedAt { get; set; }

    
}
