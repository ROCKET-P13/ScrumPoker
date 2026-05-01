using System.Text.Json;

namespace ScrumPokerAPI.Models;

public sealed class ClientWebSocketCommandMessage
{
	public string Type { get; set; } = string.Empty;

	public string Action { get; set; } = string.Empty;

	public string? RequestId { get; set; }

	public JsonElement? Payload { get; set; }
}
