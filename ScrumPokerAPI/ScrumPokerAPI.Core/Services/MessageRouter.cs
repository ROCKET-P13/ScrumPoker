using ScrumPokerAPI.Core.Interfaces;
using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Services;

public class MessageRouter(IWebSocketClient webSocketClient)
{
	private readonly IWebSocketClient _webSocketClient = webSocketClient;

	public async Task Route(SocketRequest request)
	{
		switch (request.RouteKey)
		{
			case "$connect":
				Console.WriteLine($"Connected: {request.ConnectionId}");
				break;

			case "$disconnect":
				Console.WriteLine($"Disconnected: {request.ConnectionId}");
				break;

			default:
				Console.WriteLine($"Message: {request.Body}");
				break;
		}

		await Task.CompletedTask;
	}
}