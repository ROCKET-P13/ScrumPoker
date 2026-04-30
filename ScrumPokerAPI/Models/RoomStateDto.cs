namespace ScrumPokerAPI.Models;

public class RoomStateDTO
{
	public string RoomCode { get; set; } = string.Empty;

	public bool IsRevealed { get; set; }

	public List<ParticipantStateDTO> Participants { get; set; } = new();
}