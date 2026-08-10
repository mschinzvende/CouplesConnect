using Azure.Communication.Identity;

namespace CouplesConnect.Services;

public sealed class AzureCommunicationCallService
{
    private readonly CommunicationIdentityClient? _identityClient;

    public AzureCommunicationCallService(IConfiguration configuration)
    {
        var connectionString = configuration["AzureCommunication:ConnectionString"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            _identityClient = new CommunicationIdentityClient(connectionString);
        }
    }

    public async Task<AzureCallTokenResult?> CreateVoipTokenAsync()
    {
        if (_identityClient is null)
        {
            return null;
        }

        var tokenResponse = await _identityClient.CreateUserAndTokenAsync(
            scopes: new[] { CommunicationTokenScope.VoIP });

        return new AzureCallTokenResult(
            tokenResponse.Value.AccessToken.Token,
            tokenResponse.Value.AccessToken.ExpiresOn);
    }
}

public sealed record AzureCallTokenResult(string Token, DateTimeOffset ExpiresOn);
