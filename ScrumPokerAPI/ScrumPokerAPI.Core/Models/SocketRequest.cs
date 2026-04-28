namespace ScrumPokerAPI.Core.Models;

public class SocketRequest
{
	public string? ConnectionId { get; set; }
	public string? RouteKey { get; set; }
	public string? Body { get; set; }
}