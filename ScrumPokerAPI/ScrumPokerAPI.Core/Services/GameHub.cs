using System.Text.Json;
using ScrumPokerAPI.Core.Interfaces;

namespace ScrumPokerAPI.Core.Services;

public class GameHub(RoomService roomService, IWebSocketClient webSocketClient)
{
	private readonly RoomService _roomService = roomService;
    private readonly IWebSocketClient _webSocketClient = webSocketClient;
	private readonly JsonSerializerOptions jsonSerializerOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	public async Task BroadcastRoom(string roomId)
	{
		var room = _roomService.GetRoom(roomId);

		var payload = JsonSerializer.Serialize(new
			{
				type = "ROOM_STATE",
				room
			}, jsonSerializerOptions);

		foreach (var player in room.Players)
		{
			await _webSocketClient.SendMessageAsync(player.ConnectionId, payload);
		}
	}
}