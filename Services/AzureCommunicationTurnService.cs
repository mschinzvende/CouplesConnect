using Azure.Communication.NetworkTraversal;

namespace CouplesConnect.Services;

public sealed class AzureCommunicationTurnService
{
    private readonly CommunicationRelayClient? _relayClient;
    private readonly CommunicationRelayClient? _standardEndpointRelayClient;
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
            var standardEndpointConnectionString = GetStandardEndpointConnectionString(connectionString);

            if (!string.Equals(standardEndpointConnectionString, connectionString, StringComparison.Ordinal))
            {
                _standardEndpointRelayClient = new CommunicationRelayClient(standardEndpointConnectionString);
            }
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
            var relayConfiguration = await GetRelayConfigurationAsync(_relayClient);

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
            when (exception.Status == 404 && _standardEndpointRelayClient is not null)
        {
            _logger.LogWarning(
                "The regional Azure Communication Services endpoint returned 404 for TURN. Retrying against the standard ACS endpoint.");

            try
            {
                var relayConfiguration = await GetRelayConfigurationAsync(_standardEndpointRelayClient);
                var iceServers = relayConfiguration.Value.IceServers
                    .Select(server => new WebRtcIceServer
                    {
                        Urls = server.Urls.ToList(),
                        Username = server.Username,
                        Credential = server.Credential
                    })
                    .ToList();

                _logger.LogInformation(
                    "Azure Communication Services returned {IceServerCount} ICE server(s) from the standard endpoint.",
                    iceServers.Count);

                return new AzureTurnConfigurationResult(
                    iceServers,
                    "Azure TURN relay credentials were loaded successfully.");
            }
            catch (Azure.RequestFailedException fallbackException)
            {
                _logger.LogError(
                    fallbackException,
                    "The standard Azure Communication Services endpoint also failed to retrieve TURN credentials.");

                return new AzureTurnConfigurationResult(
                    Array.Empty<WebRtcIceServer>(),
                    $"Azure TURN request failed against both ACS endpoints (HTTP {fallbackException.Status}, error code {fallbackException.ErrorCode ?? "unknown"}).");
            }
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

    private static Task<Azure.Response<CommunicationRelayConfiguration>> GetRelayConfigurationAsync(
        CommunicationRelayClient relayClient) =>
        relayClient.GetRelayConfigurationAsync(
            communicationUser: null,
            routeType: RouteType.Any,
            ttl: 3600);

    private static string GetStandardEndpointConnectionString(string connectionString)
    {
        const string endpointPrefix = "endpoint=";
        var endpointStart = connectionString.IndexOf(endpointPrefix, StringComparison.OrdinalIgnoreCase);

        if (endpointStart < 0)
        {
            return connectionString;
        }

        var endpointValueStart = endpointStart + endpointPrefix.Length;
        var endpointValueEnd = connectionString.IndexOf(';', endpointValueStart);

        if (endpointValueEnd < 0)
        {
            endpointValueEnd = connectionString.Length;
        }

        var endpointValue = connectionString[endpointValueStart..endpointValueEnd];

        if (!Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpointUri))
        {
            return connectionString;
        }

        var hostLabels = endpointUri.Host.Split('.');
        if (hostLabels.Length != 5 ||
            !hostLabels[2].Equals("communication", StringComparison.OrdinalIgnoreCase) ||
            !hostLabels[3].Equals("azure", StringComparison.OrdinalIgnoreCase) ||
            !hostLabels[4].Equals("com", StringComparison.OrdinalIgnoreCase) ||
            !endpointUri.Host.EndsWith(".communication.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var standardEndpoint = new UriBuilder(endpointUri)
        {
            Host = $"{hostLabels[0]}.communication.azure.com"
        }.Uri.ToString();

        return string.Concat(
            connectionString.AsSpan(0, endpointValueStart),
            standardEndpoint,
            connectionString.AsSpan(endpointValueEnd));
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
