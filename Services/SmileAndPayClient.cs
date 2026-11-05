using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CouplesConnect.Services;

public interface ISmileAndPayClient
{
    Task<bool> IsConfiguredAsync();
    Task<SmileAndPayInitiationResult> InitiateAsync(
        SmileAndPayInitiationRequest request,
        CancellationToken cancellationToken = default);
    Task<SmileAndPayStatusResult> CheckStatusAsync(
        string orderReference,
        CancellationToken cancellationToken = default);
}

public class SmileAndPayClient : ISmileAndPayClient
{
    private readonly HttpClient _httpClient;
    private readonly SmileAndPayOptions _options;
    private readonly SettingsService _settings;
    private readonly ILogger<SmileAndPayClient> _logger;

    public SmileAndPayClient(
        HttpClient httpClient,
        IOptions<SmileAndPayOptions> options,
        SettingsService settings,
        ILogger<SmileAndPayClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var options = await GetOptionsAsync();
        try
        {
            EnsureConfigured(options);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public async Task<SmileAndPayInitiationResult> InitiateAsync(
        SmileAndPayInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        var options = await GetOptionsAsync();
        EnsureConfigured(options);

        using var message = CreateRequest(HttpMethod.Post, "payments/initiate-transaction", options);
        message.Content = JsonContent.Create(request);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Smile & Pay initiation failed with HTTP status {StatusCode} for {OrderReference}",
                (int)response.StatusCode,
                request.OrderReference);
            return new(false, null, null, $"The payment gateway returned HTTP {(int)response.StatusCode}.");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var responseCode = FindString(root, "responseCode");
            var paymentUrl = FindString(root, "paymentUrl");
            var transactionReference = FindString(root, "transactionReference") ??
                                       FindString(root, "reference");
            var responseMessage = FindString(root, "responseMessage") ?? "Unable to initiate payment.";

            if (responseCode != "00" || !IsAllowedCheckoutUrl(paymentUrl, options))
            {
                return new(false, null, transactionReference, responseMessage);
            }

            return new(true, paymentUrl, transactionReference, responseMessage);
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Smile & Pay returned invalid JSON for {OrderReference}", request.OrderReference);
            return new(false, null, null, "The payment gateway returned an invalid response.");
        }
    }

    public async Task<SmileAndPayStatusResult> CheckStatusAsync(
        string orderReference,
        CancellationToken cancellationToken = default)
    {
        var options = await GetOptionsAsync();
        EnsureConfigured(options);

        var escapedReference = Uri.EscapeDataString(orderReference);
        using var message = CreateRequest(
            HttpMethod.Get,
            $"payments/transaction/{escapedReference}/status/check", options);
        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Smile & Pay status check failed with HTTP status {StatusCode} for {OrderReference}",
                (int)response.StatusCode,
                orderReference);
            return new(SmileAndPayPaymentState.Unknown, null, null, null,
                $"The payment gateway returned HTTP {(int)response.StatusCode}.");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var status = FindString(root, "status") ??
                         FindString(root, "transactionStatus") ??
                         FindString(root, "paymentStatus");
            var responseCode = FindString(root, "responseCode");
            var returnedOrderReference = FindString(root, "orderReference");
            var amount = FindDecimal(root, "amount");
            var currencyCode = FindString(root, "currencyCode");
            var messageText = FindString(root, "responseMessage") ?? FindString(root, "message");

            var state = MapState(status, responseCode);
            return new(state, returnedOrderReference, amount, currencyCode, messageText);
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "Smile & Pay returned invalid status JSON for {OrderReference}", orderReference);
            return new(SmileAndPayPaymentState.Unknown, null, null, null,
                "The payment gateway returned an invalid response.");
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath, SmileAndPayOptions options)
    {
        var baseUrl = options.BaseUrl.TrimEnd('/') + "/";
        var message = new HttpRequestMessage(method, new Uri(new Uri(baseUrl), relativePath));
        message.Headers.Add("x-api-key", options.ApiKey);
        message.Headers.Add("x-api-secret", options.ApiSecret);
        return message;
    }

    private static bool IsAllowedCheckoutUrl(string? value, SmileAndPayOptions options)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
               uri.Scheme == Uri.UriSchemeHttps &&
               uri.Host.Equals(options.CheckoutHost, StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureConfigured(SmileAndPayOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey) || string.IsNullOrWhiteSpace(options.ApiSecret))
        {
            throw new InvalidOperationException("Smile & Pay credentials have not been configured.");
        }

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) ||
            baseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("Smile & Pay Base URL must be a valid HTTPS URL.");
        }

        if (string.IsNullOrWhiteSpace(options.CheckoutHost) ||
            options.CheckoutHost.Contains('/') || options.CheckoutHost.Contains(':'))
        {
            throw new InvalidOperationException("Smile & Pay checkout host is invalid.");
        }

        if (options.CurrencyCode is not ("840" or "924"))
        {
            throw new InvalidOperationException("Smile & Pay currency must be USD (840) or ZWG (924).");
        }
    }

    private async Task<SmileAndPayOptions> GetOptionsAsync()
    {
        var settings = await _settings.GetAsync();
        return new SmileAndPayOptions
        {
            BaseUrl = string.IsNullOrWhiteSpace(settings.SmileAndPayBaseUrl)
                ? _options.BaseUrl
                : settings.SmileAndPayBaseUrl,
            ApiKey = string.IsNullOrWhiteSpace(settings.SmileAndPayApiKey)
                ? _options.ApiKey
                : settings.SmileAndPayApiKey,
            ApiSecret = string.IsNullOrWhiteSpace(settings.SmileAndPayApiSecret)
                ? _options.ApiSecret
                : settings.SmileAndPayApiSecret,
            CurrencyCode = string.IsNullOrWhiteSpace(settings.SmileAndPayCurrencyCode)
                ? _options.CurrencyCode
                : settings.SmileAndPayCurrencyCode,
            CheckoutHost = string.IsNullOrWhiteSpace(settings.SmileAndPayCheckoutHost)
                ? _options.CheckoutHost
                : settings.SmileAndPayCheckoutHost
        };
    }

    private static SmileAndPayPaymentState MapState(string? status, string? responseCode)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            return status.Trim().ToUpperInvariant() switch
            {
                "PAID" or "SUCCESS" or "SUCCESSFUL" or "COMPLETED" => SmileAndPayPaymentState.Paid,
                "PENDING" or "PROCESSING" or "INITIATED" => SmileAndPayPaymentState.Pending,
                "CANCELLED" or "CANCELED" => SmileAndPayPaymentState.Cancelled,
                "FAILED" or "DECLINED" or "EXPIRED" => SmileAndPayPaymentState.Failed,
                _ => SmileAndPayPaymentState.Unknown
            };
        }

        return responseCode switch
        {
            "00" => SmileAndPayPaymentState.Paid,
            "01" => SmileAndPayPaymentState.Pending,
            "08" => SmileAndPayPaymentState.Cancelled,
            "02" or "03" or "07" or "09" or "10" or "12" => SmileAndPayPaymentState.Failed,
            _ => SmileAndPayPaymentState.Unknown
        };
    }

    private static string? FindString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind switch
                    {
                        JsonValueKind.String => property.Value.GetString(),
                        JsonValueKind.Number => property.Value.GetRawText(),
                        _ => null
                    };
                }

                var nested = FindString(property.Value, propertyName);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = FindString(item, propertyName);
                if (nested is not null)
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static decimal? FindDecimal(JsonElement element, string propertyName)
    {
        var value = FindString(element, propertyName);
        return decimal.TryParse(value, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var amount)
            ? amount
            : null;
    }
}

public record SmileAndPayInitiationRequest(
    string OrderReference,
    decimal Amount,
    string ReturnUrl,
    string ResultUrl,
    string ItemName,
    string ItemDescription,
    string CurrencyCode,
    string? FirstName,
    string? LastName,
    string? Email,
    string? MobilePhoneNumber);

public record SmileAndPayInitiationResult(
    bool IsSuccessful,
    string? PaymentUrl,
    string? TransactionReference,
    string Message);

public record SmileAndPayStatusResult(
    SmileAndPayPaymentState State,
    string? OrderReference,
    decimal? Amount,
    string? CurrencyCode,
    string? Message);

public enum SmileAndPayPaymentState
{
    Unknown,
    Pending,
    Paid,
    Failed,
    Cancelled
}
