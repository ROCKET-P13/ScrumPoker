using System.Text.Json;
using ScrumPokerAPI.Core.Handlers;
using ScrumPokerAPI.Core.Messages;
using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Services;

public class HandlerRegistry(JoinRoomHandler joinRoomHandler)
{
	private readonly JoinRoomHandler _joinRoomHandler = joinRoomHandler;

	public async Task Dispatch(SocketRequest request)
	{
		using var doc = JsonDocument.Parse(request.Body!);
		var type = doc.RootElement.GetProperty("type").GetString();

		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		switch (type)
		{
			case "JOIN_ROOM":
				var join = JsonSerializer.Deserialize<JoinRoomMessage>(request.Body!, options);
				await _joinRoomHandler.Handle(join!, request);
				break;

			default:
				Console.WriteLine($"Unknown Type: {type}");
				break;
		}
	}
}