namespace ScrumPokerAPI.Core.Messages;

public class ResetRoundMessage : ClientMessage
{
	public string RoomId { get; set; } = default!;
}