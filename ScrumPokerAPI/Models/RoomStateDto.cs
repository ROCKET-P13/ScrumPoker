using System.Text.Json.Serialization;

namespace ScrumPokerAPI.Models;

public class RoomStateDto
{
	public string RoomCode { get; set; } = string.Empty;

	public bool IsRevealed { get; set; }

	public List<ParticipantStateDto> Participants { get; set; } = new();
}

public class ParticipantStateDto
{
	public string DisplayName { get; set; } = string.Empty;

	public bool HasVoted { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Vote { get; set; }
}
