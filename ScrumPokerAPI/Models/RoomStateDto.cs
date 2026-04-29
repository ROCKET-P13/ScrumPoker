using System.Text.Json.Serialization;

namespace ScrumPokerAPI.Models;

public class RoomStateDto
{
	public string RoomCode { get; set; } = string.Empty;

	public bool Revealed { get; set; }

	public List<ParticipantStateDto> Participants { get; set; } = new();
}

public class ParticipantStateDto
{
	public string DisplayName { get; set; } = string.Empty;

	public bool HasVoted { get; set; }

	/// <summary>Present only when <see cref="RoomStateDto.Revealed"/> is true.</summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Vote { get; set; }
}
