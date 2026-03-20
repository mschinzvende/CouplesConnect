using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CouplesConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Data.Models;

public partial class SupportTicket
{
    [Key]
    public Guid TicketId { get; set; }

    public Guid UserId { get; set; }

    [StringLength(200)]
    public string? Subject { get; set; }

    public string? Message { get; set; }

    [StringLength(50)]
    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

   
}
