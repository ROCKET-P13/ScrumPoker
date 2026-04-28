namespace ScrumPokerAPI.Core.Models;

public class Room
{
	public string Id { get; set; } = default!;
	public List<Player> Players { get; set; } = [];
	public bool IsRevealed { get; set; }
	public Dictionary<string, string?> Votes { get; set; } = [];
}