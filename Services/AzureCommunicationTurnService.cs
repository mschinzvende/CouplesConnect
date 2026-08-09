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

    public async Task<IReadOnlyList<WebRtcIceServer>> GetIceServersAsync()
    {
        if (_relayClient is null)
        {
            return Array.Empty<WebRtcIceServer>();
        }

        try
        {
            var relayConfiguration = await _relayClient.GetRelayConfigurationAsync();

            return relayConfiguration.Value.IceServers
                .Select(server => new WebRtcIceServer
                {
                    Urls = server.Urls.ToList(),
                    Username = server.Username,
                    Credential = server.Credential
                })
                .ToList();
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unable to retrieve temporary TURN credentials from Azure Communication Services.");

            return Array.Empty<WebRtcIceServer>();
        }
    }
}

public sealed class WebRtcIceServer
{
    public List<string> Urls { get; init; } = new();
    public string? Username { get; init; }
    public string? Credential { get; init; }
}
