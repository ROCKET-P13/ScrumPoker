using System.Text.Json.Serialization;

namespace ScrumPokerAPI.Models;

public class ParticipantStateDTO
{
	public string DisplayName { get; set; } = string.Empty;

	public bool HasVoted { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Vote { get; set; }
}
