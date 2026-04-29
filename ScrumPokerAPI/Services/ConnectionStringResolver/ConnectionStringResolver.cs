using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace ScrumPokerAPI.Services.ConnectionStringResolver;

public static class ConnectionStringResolver
{
    private static string? _cachedConnectionString;

    /// <summary>
    /// Resolves PostgreSQL connection string: env <c>ConnectionStrings__DefaultConnection</c>,
    /// or secret plain text when <c>DATABASE_SECRET_ARN</c> is set.
    /// </summary>
    public static async Task<string> ResolveAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedConnectionString != null)
            return _cachedConnectionString;

        var directConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(directConnectionString))
        {
            _cachedConnectionString = directConnectionString.Trim();
            return _cachedConnectionString;
        }

        var secretArn = Environment.GetEnvironmentVariable("DATABASE_SECRET_ARN");
        if (string.IsNullOrWhiteSpace(secretArn))
            throw new InvalidOperationException(
                "Set ConnectionStrings__DefaultConnection or DATABASE_SECRET_ARN for PostgreSQL.");

        using var client = new AmazonSecretsManagerClient();
        var response = await client.GetSecretValueAsync(
            new GetSecretValueRequest { SecretId = secretArn.Trim() },
            cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(response.SecretString))
            throw new InvalidOperationException("Secret has no SecretString payload.");

        _cachedConnectionString = response.SecretString.Trim();
        return _cachedConnectionString;
    }
}
