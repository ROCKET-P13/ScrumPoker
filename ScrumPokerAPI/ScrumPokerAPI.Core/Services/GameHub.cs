using ScrumPokerAPI.Core.Interfaces;
using ScrumPokerAPI.Core.Serialization;

namespace ScrumPokerAPI.Core.Services;

public class GameHub(RoomService roomService, IWebSocketClient webSocketClient)
{
	private readonly RoomService _roomService = roomService;
	private readonly IWebSocketClient _webSocketClient = webSocketClient;

	public async Task BroadcastRoom(string roomId)
	{
		if (string.IsNullOrWhiteSpace(roomId))
		{
			return;
		}

		if (!_roomService.TryGetRoom(roomId, out var room) || room is null)
		{
			return;
		}

		var payload = RoomStatePayload.Serialize(room);

		foreach (var player in room.Players)
		{
			await _webSocketClient.SendMessageAsync(player.ConnectionId, payload);
		}
	}
}
