namespace CouplesConnect.Services;

public class SmileAndPayOptions
{
    public const string SectionName = "SmileAndPay";

    public string BaseUrl { get; set; } =
        "https://zbnet.zb.co.zw/wallet_sandbox_api/payments-gateway";

    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "840";
    public string CheckoutHost { get; set; } = "zbnet.zb.co.zw";
}
