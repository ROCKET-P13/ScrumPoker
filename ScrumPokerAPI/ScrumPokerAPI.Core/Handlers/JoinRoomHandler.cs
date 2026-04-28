using System.Text.Json;
using ScrumPokerAPI.Core.Interfaces;
using ScrumPokerAPI.Core.Messages;
using ScrumPokerAPI.Core.Models;
using ScrumPokerAPI.Core.Services;

namespace ScrumPokerAPI.Core.Handlers;

public class JoinRoomHandler(IWebSocketClient webSocketClient, RoomService roomService)
{
	private readonly IWebSocketClient _webSocketClient = webSocketClient;
	private readonly RoomService _roomService = roomService;

	public async Task Handle(JoinRoomMessage message, SocketRequest socketRequest)
	{
		var room = _roomService.GetOrCreateRoom(message.RoomId);

		var player = new Player
		{
			ConnectionId = socketRequest.ConnectionId,
			Name = message.Name
		};

		_roomService.AddPlayer(message.RoomId, player);

		var payload = JsonSerializer.Serialize(new
		{
			type = "ROOM_STATE",
			room
		});

		foreach (var p in room.Players)
		{
			await _webSocketClient.SendMessageAsync(p.ConnectionId, payload);
		}
	}
}