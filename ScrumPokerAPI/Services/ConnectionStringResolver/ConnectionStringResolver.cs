using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Npgsql;

namespace ScrumPokerAPI.Services.ConnectionStringResolver;

public static class ConnectionStringResolver
{
	private static string? _cachedConnectionString;

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

		var secretArn = Environment.GetEnvironmentVariable("DATABASE_SECRET_ARN")
			?? Environment.GetEnvironmentVariable("POSTGRES_SECRET_ARN");
		if (string.IsNullOrWhiteSpace(secretArn))
		{
			throw new InvalidOperationException(
				"Set ConnectionStrings__DefaultConnection, DATABASE_SECRET_ARN, or POSTGRES_SECRET_ARN for PostgreSQL.");
		}

		using var client = new AmazonSecretsManagerClient();
		var response = await client.GetSecretValueAsync(
			new GetSecretValueRequest { SecretId = secretArn.Trim() },
			cancellationToken).ConfigureAwait(false);

		if (string.IsNullOrWhiteSpace(response.SecretString))
			throw new InvalidOperationException("Secret has no SecretString payload.");

		var secretPayload = response.SecretString.Trim();
		_cachedConnectionString = TryExtractFromAppsettingsJson(secretPayload)
			?? TryBuildFromRdsSecretJson(secretPayload)
			?? secretPayload;
		return _cachedConnectionString;
	}

	private static string? TryExtractFromAppsettingsJson(string secretPayload)
	{
		JsonDocument doc;
		try
		{
			doc = JsonDocument.Parse(secretPayload);
		}
		catch (JsonException)
		{
			return null;
		}

		using (doc)
		{
			if (doc.RootElement.ValueKind != JsonValueKind.Object)
				return null;

			foreach (var section in doc.RootElement.EnumerateObject())
			{
				if (!string.Equals(section.Name, "ConnectionStrings", StringComparison.OrdinalIgnoreCase))
					continue;
				if (section.Value.ValueKind != JsonValueKind.Object)
					return null;
				foreach (var entry in section.Value.EnumerateObject())
				{
					if (!string.Equals(entry.Name, "DefaultConnection", StringComparison.OrdinalIgnoreCase))
						continue;
					if (entry.Value.ValueKind != JsonValueKind.String)
						return null;
					var s = entry.Value.GetString();
					return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
				}
			}

			return null;
		}
	}

	private static string? TryBuildFromRdsSecretJson(string secretPayload)
	{
		JsonDocument doc;
		try
		{
			doc = JsonDocument.Parse(secretPayload);
		}
		catch (JsonException)
		{
			return null;
		}

		using (doc)
		{
			if (doc.RootElement.ValueKind != JsonValueKind.Object)
				return null;

			if (!TryGetStringProperty(doc.RootElement, "username", out var username)
				|| !TryGetStringProperty(doc.RootElement, "password", out var password))
				return null;

			var host = Environment.GetEnvironmentVariable("DB_HOST");
			var portText = Environment.GetEnvironmentVariable("DB_PORT");
			var database = Environment.GetEnvironmentVariable("DB_NAME");
			if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(portText) || string.IsNullOrWhiteSpace(database))
			{
				throw new InvalidOperationException(
					"Secret is RDS-style username/password JSON; set DB_HOST, DB_PORT, and DB_NAME on the Lambda.");
			}

			if (!int.TryParse(portText.Trim(), out var port) || port < 1 || port > 65535)
				throw new InvalidOperationException("DB_PORT must be an integer between 1 and 65535.");

			var builder = new NpgsqlConnectionStringBuilder
			{
				Host = host.Trim(),
				Port = port,
				Database = database.Trim(),
				Username = username,
				Password = password,
				SslMode = SslMode.Require,
			};
			return builder.ConnectionString;
		}
	}

	private static bool TryGetStringProperty(JsonElement obj, string name, out string value)
	{
		value = string.Empty;
		if (!obj.TryGetProperty(name, out var prop))
			return false;
		if (prop.ValueKind != JsonValueKind.String)
			return false;
		var s = prop.GetString();
		if (string.IsNullOrEmpty(s))
			return false;
		value = s;
		return true;
	}
}
