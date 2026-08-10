using Azure.Communication.NetworkTraversal;

namespace CouplesConnect.Services;

public sealed class AzureCommunicationTurnService
{
    private readonly CommunicationRelayClient? _relayClient;
    private readonly ILogger<AzureCommunicationTurnService> _logger;

    public AzureCommunicationTurnService(
        IConfiguration configuration,
        ILogger<AzureCommunicationTurnService> logger)
    {
        _logger = logger;

        var connectionString = configuration["AzureCommunication:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _relayClient = new CommunicationRelayClient(connectionString);
        }
        else
        {
            _logger.LogWarning(
                "Azure Communication Services TURN is not configured. Set AzureCommunication__ConnectionString in the host environment.");
        }
    }

    public async Task<AzureTurnConfigurationResult> GetIceServersAsync()
    {
        if (_relayClient is null)
        {
            return new AzureTurnConfigurationResult(
                Array.Empty<WebRtcIceServer>(),
                "Azure TURN is unavailable because AzureCommunication__ConnectionString is missing from this app instance.");
        }

        try
        {
            // Explicitly use the current relay-issuance operation's recommended route and
            // a one-hour credential lifetime. The default overload can target an older
            // operation on legacy SDKs.
            var relayConfiguration = await _relayClient.GetRelayConfigurationAsync(
                communicationUser: null,
                routeType: RouteType.Any,
                ttl: 3600);

            var iceServers = relayConfiguration.Value.IceServers
                .Select(server => new WebRtcIceServer
                {
                    Urls = server.Urls.ToList(),
                    Username = server.Username,
                    Credential = server.Credential
                })
                .ToList();

            var hasTurnServer = iceServers.Any(server =>
                server.Urls.Any(url => url.StartsWith("turn:", StringComparison.OrdinalIgnoreCase) ||
                                       url.StartsWith("turns:", StringComparison.OrdinalIgnoreCase)));

            if (hasTurnServer)
            {
                _logger.LogInformation(
                    "Azure Communication Services returned {IceServerCount} ICE server(s), including TURN relay URLs.",
                    iceServers.Count);

                return new AzureTurnConfigurationResult(
                    iceServers,
                    "Azure TURN relay credentials were loaded successfully.");
            }

            _logger.LogWarning(
                "Azure Communication Services returned {IceServerCount} ICE server(s), but none included a TURN relay URL.",
                iceServers.Count);

            return new AzureTurnConfigurationResult(
                iceServers,
                "Azure returned no usable TURN relay URLs. Check the App Service Log stream for the ACS response details.");
        }
        catch (Azure.RequestFailedException exception)
        {
            _logger.LogError(
                exception,
                "Unable to retrieve temporary TURN credentials from Azure Communication Services.");

            return new AzureTurnConfigurationResult(
                Array.Empty<WebRtcIceServer>(),
                $"Azure TURN request failed (HTTP {exception.Status}, error code {exception.ErrorCode ?? "unknown"}).");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unable to retrieve temporary TURN credentials from Azure Communication Services.");

            return new AzureTurnConfigurationResult(
                Array.Empty<WebRtcIceServer>(),
                $"Azure TURN request failed ({exception.GetType().Name}).");
        }
    }
}

public sealed record AzureTurnConfigurationResult(
    IReadOnlyList<WebRtcIceServer> IceServers,
    string Status);

public sealed class WebRtcIceServer
{
    public List<string> Urls { get; init; } = new();
    public string? Username { get; init; }
    public string? Credential { get; init; }
}
