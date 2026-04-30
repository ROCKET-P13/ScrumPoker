namespace ScrumPokerAPI.Models;

public class RoomStateViewModel
{
	public string RoomCode { get; set; } = string.Empty;

	public bool IsRevealed { get; set; }

	public List<ParticipantViewModel> Participants { get; set; } = [];
}