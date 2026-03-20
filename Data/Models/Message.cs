using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CouplesConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

public partial class Message
{
    [Key]
    public Guid MessageId { get; set; }

    public Guid BookingId { get; set; }

    public Guid SenderId { get; set; }

    public string? MessageText { get; set; }

    public bool? IsAnonymous { get; set; }

    public DateTime? SentAt { get; set; }

      [ForeignKey("SenderId")]

    public ApplicationUser User { get; set; }
}
