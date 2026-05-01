namespace ScrumPokerAPI.Models;

public static class WebSocketEnvelopeType
{
	public const string Command = "command";

	public const string Response = "response";

	public const string Event = "event";
}

public static class WebSocketEventNames
{
	public const string RoomState = "ROOM_STATE";
}
