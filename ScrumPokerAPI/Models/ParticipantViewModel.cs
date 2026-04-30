using System.Text.Json.Serialization;

namespace ScrumPokerAPI.Models;

public class ParticipantViewModel
{
	public string DisplayName { get; set; } = string.Empty;

	public bool IsRoomAdmin { get; set; }

	public bool HasVoted { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Vote { get; set; }
}
