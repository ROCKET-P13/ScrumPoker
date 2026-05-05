namespace ScrumPokerAPI.Models;

public sealed class CreateRoomCommandPayload
{
	public string? DisplayName { get; set; }
}

public sealed class JoinRoomCommandPayload
{
	public string? RoomCode { get; set; }

	public string? DisplayName { get; set; }
	public bool IsRoomAdmin { get; set; } = false;
}

public sealed class SendVoteCommandPayload
{
	public string? Value { get; set; }
}
