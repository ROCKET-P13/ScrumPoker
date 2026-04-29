using System.Text.Json;

namespace ScrumPokerAPI.Serialization;

public static class AppJsonSerializerOptions
{
    public static readonly JsonSerializerOptions ApplicationDefault = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };
}
