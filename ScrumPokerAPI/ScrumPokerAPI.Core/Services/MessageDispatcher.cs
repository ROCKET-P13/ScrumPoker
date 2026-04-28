using System.Text.Json;
using ScrumPokerAPI.Core.Interfaces;
using ScrumPokerAPI.Core.Messages;
using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Services;

public class MessageDispatcher(IWebSocketClient webSocketClient)
{
	private readonly IWebSocketClient _webSocketClient = webSocketClient;

	public async Task Dispatch(SocketRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Body))
		{
			return;
		}

		var baseMessage = JsonSerializer.Deserialize<ClientMessage>(request.Body);

		if (baseMessage?.Type is null)
		{
			return;
		}

		switch (baseMessage.Type)
		{
			case "JOIN_ROOM":
				{
					var message = JsonSerializer.Deserialize<JoinRoomMessage>(request.Body);
					Console.WriteLine($"JOIN ROOM: {message!.RoomId} by ${message.Name}");
					break;
				}

			case "SEND_VOTE":
				{
					var message = JsonSerializer.Deserialize<SendVoteMessage>(request.Body);
					Console.WriteLine($"SEND VOTE: ${message!.Vote}");
					break;
				}

			default:
				Console.WriteLine($"Unknown Type: ${baseMessage.Type}");
				break;
		}

		await Task.CompletedTask;
	}
}