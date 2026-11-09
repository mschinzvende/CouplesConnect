using System.Data;
using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using Microsoft.EntityFrameworkCore;

namespace CouplesConnect.Services;

public class SubscriptionPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly ISmileAndPayClient _gateway;
    private readonly ILogger<SubscriptionPaymentService> _logger;

    public SubscriptionPaymentService(
        ApplicationDbContext context,
        ISmileAndPayClient gateway,
        ILogger<SubscriptionPaymentService> logger)
    {
        _context = context;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task<PaymentReconciliationResult> ReconcileAsync(
        string orderReference,
        CancellationToken cancellationToken = default)
    {
        var payment = await _context.SubscriptionPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderReference == orderReference, cancellationToken);

        if (payment is null)
        {
            return new(PaymentReconciliationOutcome.NotFound, "Payment not found.");
        }

        if (payment.Status == SubscriptionPaymentStatuses.Paid)
        {
            return new(PaymentReconciliationOutcome.Paid, "Your subscription is already active.");
        }

        SmileAndPayStatusResult gatewayStatus;
        try
        {
            gatewayStatus = await _gateway.CheckStatusAsync(orderReference, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(exception, "Unable to reconcile Smile & Pay order {OrderReference}", orderReference);
            return new(PaymentReconciliationOutcome.Unavailable,
                "We could not confirm the payment yet. Please try again shortly.");
        }

        if (gatewayStatus.OrderReference is not null &&
            !gatewayStatus.OrderReference.Equals(orderReference, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Smile & Pay returned mismatched order reference {ReturnedReference} for {OrderReference}",
                gatewayStatus.OrderReference,
                orderReference);
            return new(PaymentReconciliationOutcome.Unavailable,
                "The gateway returned an invalid payment reference.");
        }

        if (gatewayStatus.Amount.HasValue && gatewayStatus.Amount.Value != payment.Amount)
        {
            _logger.LogWarning(
                "Smile & Pay amount mismatch for {OrderReference}: expected {Expected}, got {Actual}",
                orderReference,
                payment.Amount,
                gatewayStatus.Amount.Value);
            return new(PaymentReconciliationOutcome.Unavailable,
                "The confirmed payment amount does not match this package.");
        }

        if (gatewayStatus.CurrencyCode is not null &&
            !gatewayStatus.CurrencyCode.Equals(payment.CurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Smile & Pay currency mismatch for {OrderReference}: expected {Expected}, got {Actual}",
                orderReference,
                payment.CurrencyCode,
                gatewayStatus.CurrencyCode);
            return new(PaymentReconciliationOutcome.Unavailable,
                "The confirmed payment currency does not match this package.");
        }

        return gatewayStatus.State switch
        {
            SmileAndPayPaymentState.Paid => await ActivateSubscriptionAsync(
                payment.SubscriptionPaymentId,
                gatewayStatus.Message,
                cancellationToken),
            SmileAndPayPaymentState.Pending => await UpdateStatusAsync(
                payment.SubscriptionPaymentId,
                SubscriptionPaymentStatuses.Pending,
                PaymentReconciliationOutcome.Pending,
                gatewayStatus.Message ?? "Payment is still pending.",
                cancellationToken),
            SmileAndPayPaymentState.Cancelled => await UpdateStatusAsync(
                payment.SubscriptionPaymentId,
                SubscriptionPaymentStatuses.Cancelled,
                PaymentReconciliationOutcome.Cancelled,
                gatewayStatus.Message ?? "Payment was cancelled.",
                cancellationToken),
            SmileAndPayPaymentState.Failed => await UpdateStatusAsync(
                payment.SubscriptionPaymentId,
                SubscriptionPaymentStatuses.Failed,
                PaymentReconciliationOutcome.Failed,
                gatewayStatus.Message ?? "Payment failed.",
                cancellationToken),
            _ => new(PaymentReconciliationOutcome.Unavailable,
                gatewayStatus.Message ?? "The gateway did not return a recognised payment status.")
        };
    }

    private async Task<PaymentReconciliationResult> ActivateSubscriptionAsync(
        Guid paymentId,
        string? gatewayMessage,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var payment = await _context.SubscriptionPayments
            .Include(x => x.Package)
            .FirstAsync(x => x.SubscriptionPaymentId == paymentId, cancellationToken);

        if (payment.Status == SubscriptionPaymentStatuses.Paid)
        {
            await transaction.CommitAsync(cancellationToken);
            return new(PaymentReconciliationOutcome.Paid, "Your subscription is already active.");
        }

        var now = DateTime.UtcNow;
        var unavailableSubscriptions = await _context.UserSubscriptions
            .Where(x => x.UserId == payment.UserId && x.IsActive &&
                        (x.SessionsRemaining <= 0 ||
                         (x.EndDate.HasValue && x.EndDate <= now)))
            .ToListAsync(cancellationToken);

        foreach (var unavailable in unavailableSubscriptions)
        {
            unavailable.IsActive = false;
        }

        var activeSubscription = await _context.UserSubscriptions
            .FirstOrDefaultAsync(x => x.UserId == payment.UserId && x.IsActive &&
                                      x.SessionsRemaining > 0 &&
                                      (!x.EndDate.HasValue || x.EndDate > now), cancellationToken);

        if (activeSubscription is not null)
        {
            payment.Status = SubscriptionPaymentStatuses.PaidRequiresReview;
            payment.StatusMessage = "Payment confirmed while another subscription is active.";
            payment.PaidAt = now;
            payment.UpdatedAt = now;
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogError(
                "Paid order {OrderReference} could not be applied because subscription {SubscriptionId} is active",
                payment.OrderReference,
                activeSubscription.SubscriptionId);
            return new(PaymentReconciliationOutcome.RequiresReview,
                "Payment was received, but your existing subscription is still active. Please contact support.");
        }

        var subscription = new UserSubscription
        {
            UserId = payment.UserId,
            PackageId = payment.PackageId,
            SessionsRemaining = payment.Package.SessionsPerMonth,
            IsTrial = false,
            IsActive = true,
            StartDate = now,
            EndDate = now.AddMonths(1),
            AmountPaid = payment.Amount
        };

        _context.UserSubscriptions.Add(subscription);
        await _context.SaveChangesAsync(cancellationToken);

        payment.SubscriptionId = subscription.SubscriptionId;
        payment.Status = SubscriptionPaymentStatuses.Paid;
        payment.StatusMessage = gatewayMessage ?? "Payment confirmed.";
        payment.PaidAt = now;
        payment.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new(PaymentReconciliationOutcome.Paid,
            $"Payment confirmed. {payment.Package.SessionsPerMonth} session credits were added to your account.");
    }

    private async Task<PaymentReconciliationResult> UpdateStatusAsync(
        Guid paymentId,
        string status,
        PaymentReconciliationOutcome outcome,
        string message,
        CancellationToken cancellationToken)
    {
        var payment = await _context.SubscriptionPayments
            .FirstAsync(x => x.SubscriptionPaymentId == paymentId, cancellationToken);

        if (payment.Status != SubscriptionPaymentStatuses.Paid)
        {
            payment.Status = status;
            payment.StatusMessage = message;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new(outcome, message);
    }
}

public record PaymentReconciliationResult(PaymentReconciliationOutcome Outcome, string Message);

public enum PaymentReconciliationOutcome
{
    NotFound,
    Pending,
    Paid,
    Failed,
    Cancelled,
    RequiresReview,
    Unavailable
}
