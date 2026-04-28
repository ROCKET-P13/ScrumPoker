namespace ScrumPokerAPI.Core.Messages;

public class JoinRoomMessage : ClientMessage
{
	public string RoomId { get; set; } = default!;
	public string Name { get; set; } = default!;
}