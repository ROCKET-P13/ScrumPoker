namespace ScrumPokerAPI.Models.Requests;

public sealed class CreateRoomRequestDTO
{
    public string DisplayName { get; set; } = string.Empty;
	public bool IsPlayer { get; set; }
}
