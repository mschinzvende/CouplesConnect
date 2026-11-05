using System.Text.Json;
using CouplesConnect.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CouplesConnect.Pages.Payments;

[AllowAnonymous]
[IgnoreAntiforgeryToken]
public class SmileAndPayWebhookModel : PageModel
{
    private readonly SubscriptionPaymentService _payments;
    private readonly ILogger<SmileAndPayWebhookModel> _logger;

    public SmileAndPayWebhookModel(SubscriptionPaymentService payments,
        ILogger<SmileAndPayWebhookModel> logger)
    {
        _payments = payments;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var payload = await JsonDocument.ParseAsync(Request.Body,
                cancellationToken: cancellationToken);
            var orderReference = FindOrderReference(payload.RootElement);

            if (!string.IsNullOrWhiteSpace(orderReference))
                await _payments.ReconcileAsync(orderReference, cancellationToken);
            else
                _logger.LogWarning("Smile & Pay callback did not contain an orderReference");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to process Smile & Pay callback");
        }

        // Smile & Pay requires callbacks to be acknowledged with HTTP 200. The callback body
        // is only a notification; ReconcileAsync independently verifies status with ZB.
        return new OkResult();
    }

    private static string? FindOrderReference(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals("orderReference", StringComparison.OrdinalIgnoreCase) &&
                    property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();

                var nested = FindOrderReference(property.Value);
                if (nested is not null) return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindOrderReference(item);
                if (nested is not null) return nested;
            }
        }

        return null;
    }
}
