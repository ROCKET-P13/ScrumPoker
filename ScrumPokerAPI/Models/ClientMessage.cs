namespace ScrumPokerAPI.Models;

/// <summary>Inbound WebSocket JSON body (route <c>$default</c>).</summary>
public class ClientMessage
{
	public string Action { get; set; } = string.Empty;

	public string? DisplayName { get; set; }

	public string? RoomCode { get; set; }

	public string? Value { get; set; }
}
