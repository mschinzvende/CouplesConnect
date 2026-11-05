using CouplesConnect.Data;
using CouplesConnect.Data.Models;
using CouplesConnect.Models;
using CouplesConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CouplesConnect.Pages.User.Packages;

[Authorize(Roles = "User")]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISmileAndPayClient _gateway;
    private readonly SmileAndPayOptions _options;
    private readonly SettingsService _settings;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        ISmileAndPayClient gateway, IOptions<SmileAndPayOptions> options, SettingsService settings,
        ILogger<IndexModel> logger)
    {
        _context = context;
        _userManager = userManager;
        _gateway = gateway;
        _options = options.Value;
        _settings = settings;
        _logger = logger;
    }

    public List<Package> Packages { get; set; } = new();
    public UserSubscription? CurrentSubscription { get; set; }
    public SubscriptionPayment? PendingPayment { get; set; }

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return;

        await DeactivateExpiredSubscriptionsAsync(user.Id);

        Packages = await _context.Packages.Where(x => x.IsActive).OrderBy(x => x.Price).ToListAsync();
        CurrentSubscription = await _context.UserSubscriptions
            .Include(x => x.Package)
            .Where(x => x.UserId == user.Id && x.IsActive)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync();
        PendingPayment = await _context.SubscriptionPayments
            .Include(x => x.Package)
            .Where(x => x.UserId == user.Id && x.Status == SubscriptionPaymentStatuses.Pending)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IActionResult> OnPostPurchaseAsync(int packageId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Challenge();

        var package = await _context.Packages
            .FirstOrDefaultAsync(x => x.PackageId == packageId && x.IsActive);
        if (package is null)
        {
            TempData["Error"] = "That package is no longer available.";
            return RedirectToPage();
        }

        if (package.Price <= 0)
        {
            TempData["Error"] = "This package does not have a valid payment amount.";
            return RedirectToPage();
        }

        var platformSettings = await _settings.GetAsync();
        var currencyCode = string.IsNullOrWhiteSpace(platformSettings.SmileAndPayCurrencyCode)
            ? _options.CurrencyCode
            : platformSettings.SmileAndPayCurrencyCode;
        var checkoutHost = string.IsNullOrWhiteSpace(platformSettings.SmileAndPayCheckoutHost)
            ? _options.CheckoutHost
            : platformSettings.SmileAndPayCheckoutHost;

        await DeactivateExpiredSubscriptionsAsync(user.Id);
        if (await _context.UserSubscriptions.AnyAsync(x => x.UserId == user.Id && x.IsActive))
        {
            TempData["Error"] = "You already have an active subscription. Use its remaining sessions before subscribing again.";
            return RedirectToPage();
        }

        var pendingPayment = await _context.SubscriptionPayments.FirstOrDefaultAsync(x =>
            x.UserId == user.Id && x.Status == SubscriptionPaymentStatuses.Pending);
        if (pendingPayment is not null)
        {
            if (pendingPayment.PackageId == packageId && IsUsablePaymentUrl(pendingPayment.PaymentUrl, checkoutHost))
                return Redirect(pendingPayment.PaymentUrl!);

            TempData["Error"] = "You already have a payment in progress. Complete or cancel it before choosing another package.";
            return RedirectToPage();
        }

        if (!await _gateway.IsConfiguredAsync())
        {
            TempData["Error"] = "Online payments are not configured yet. Please contact support.";
            return RedirectToPage();
        }

        var paymentId = Guid.NewGuid();
        var orderReference = $"CC-{paymentId:N}";
        var payment = new SubscriptionPayment
        {
            SubscriptionPaymentId = paymentId,
            UserId = user.Id,
            PackageId = package.PackageId,
            Amount = package.Price,
            CurrencyCode = currencyCode,
            OrderReference = orderReference
        };

        _context.SubscriptionPayments.Add(payment);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Duplicate subscription checkout prevented for user {UserId}", user.Id);
            TempData["Error"] = "A subscription payment is already in progress.";
            return RedirectToPage();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        var returnUrl = $"{baseUrl}/User/Packages/PaymentReturn?orderReference={Uri.EscapeDataString(orderReference)}";
        var resultUrl = $"{baseUrl}/Payments/SmileAndPayWebhook";
        var (firstName, lastName) = SplitName(user.FullName);

        try
        {
            var result = await _gateway.InitiateAsync(new SmileAndPayInitiationRequest(
                orderReference, package.Price, returnUrl, resultUrl, package.Name, package.Description,
                currencyCode, firstName, lastName, user.Email, user.Phone));

            payment.GatewayTransactionReference = result.TransactionReference;
            payment.StatusMessage = result.Message;
            payment.UpdatedAt = DateTime.UtcNow;

            if (!result.IsSuccessful || !IsUsablePaymentUrl(result.PaymentUrl, checkoutHost))
            {
                payment.Status = SubscriptionPaymentStatuses.Failed;
                await _context.SaveChangesAsync();
                TempData["Error"] = result.Message;
                return RedirectToPage();
            }

            payment.PaymentUrl = result.PaymentUrl;
            await _context.SaveChangesAsync();
            return Redirect(result.PaymentUrl!);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogError(exception, "Unable to initiate Smile & Pay order {OrderReference}", orderReference);
            payment.Status = SubscriptionPaymentStatuses.Failed;
            payment.StatusMessage = "Unable to connect to the payment gateway.";
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["Error"] = "Unable to start payment. Please try again.";
            return RedirectToPage();
        }
    }

    private async Task DeactivateExpiredSubscriptionsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var expired = await _context.UserSubscriptions.Where(x => x.UserId == userId && x.IsActive &&
            x.EndDate.HasValue && x.EndDate <= now).ToListAsync();
        if (expired.Count == 0) return;

        foreach (var subscription in expired) subscription.IsActive = false;
        await _context.SaveChangesAsync();
    }

    private static bool IsUsablePaymentUrl(string? value, string checkoutHost) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
        uri.Host.Equals(checkoutHost, StringComparison.OrdinalIgnoreCase);

    private static (string? FirstName, string? LastName) SplitName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return (null, null);
        var names = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return (names[0], names.Length > 1 ? names[1] : null);
    }
}
