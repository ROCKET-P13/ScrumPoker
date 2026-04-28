namespace ScrumPokerAPI.Core.Models;

public class Player
{
	public string ConnectionId { get; set; } = default!;
	public string Name { get; set; } = default!;
	public string? Vote { get; set; }
}