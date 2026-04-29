using System.Text.Json;

namespace ScrumPokerAPI.Serialization;

/// <summary>
/// Shared JSON options: camelCase for property names (matches typical JS/clients) without per-property attributes.
/// </summary>
public static class AppJsonSerializerOptions
{
    public static readonly JsonSerializerOptions ApplicationDefault = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };
}
