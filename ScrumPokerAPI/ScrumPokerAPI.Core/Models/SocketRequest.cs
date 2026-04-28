namespace ScrumPokerAPI.Core.Models;

public class SocketRequest
{
	public string ConnectionId { get; set; } = default!;
	public string RouteKey { get; set; } = default!;
	public string? Body { get; set; }
}