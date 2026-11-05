using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CouplesConnect.Models;

namespace CouplesConnect.Data.Models;

public class SubscriptionPayment
{
    [Key]
    public Guid SubscriptionPaymentId { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int PackageId { get; set; }
    public Package Package { get; set; } = null!;

    public int? SubscriptionId { get; set; }
    public UserSubscription? Subscription { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }

    [MaxLength(3)]
    public string CurrencyCode { get; set; } = "840";

    [MaxLength(100)]
    public string OrderReference { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? GatewayTransactionReference { get; set; }

    [MaxLength(1000)]
    public string? PaymentUrl { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = SubscriptionPaymentStatuses.Pending;

    [MaxLength(500)]
    public string? StatusMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
}

public static class SubscriptionPaymentStatuses
{
    public const string Pending = "Pending";
    public const string Paid = "Paid";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
    public const string PaidRequiresReview = "PaidRequiresReview";
}
