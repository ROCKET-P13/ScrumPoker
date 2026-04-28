namespace ScrumPokerAPI.Core.Messages;

public class RevealVotesMessage : ClientMessage
{
	public string RoomId { get; set; } = default!;
}