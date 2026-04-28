using System.Text.Json;
using ScrumPokerAPI.Core.Interfaces;
using ScrumPokerAPI.Core.Messages;
using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Services;

public class MessageDispatcher(IWebSocketClient webSocketClient, RoomService roomService)
{
	private readonly IWebSocketClient _webSocketClient = webSocketClient;
	private readonly RoomService _roomService = roomService;

	public async Task Dispatch(SocketRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Body))
		{
			return;
		}

		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		using var doc = JsonDocument.Parse(request.Body!);

		var type = doc.RootElement.GetProperty("type").GetString();

		switch (type)
		{
			case "JOIN_ROOM":
				{
					Console.WriteLine($"testinggg: {type}");
					var message = JsonSerializer.Deserialize<JoinRoomMessage>(request.Body!, options);
					var room = _roomService.GetOrCreateRoom(message!.RoomId);

					var player = new Player
					{
						ConnectionId = request.ConnectionId,
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

					Console.WriteLine($"JOIN ROOM: {message!.RoomId} by {message.Name}");
					break;
				}

			case "SEND_VOTE":
				{
					var message = JsonSerializer.Deserialize<SendVoteMessage>(request.Body!, options);
					Console.WriteLine($"SEND VOTE: ${message!.Vote}");
					break;
				}

			default:
				Console.WriteLine($"Unknown Type: ${type}");
				break;
		}

		await Task.CompletedTask;
	}
}