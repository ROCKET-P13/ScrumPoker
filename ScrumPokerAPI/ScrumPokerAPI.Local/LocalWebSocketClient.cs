using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using ScrumPokerAPI.Core.Interfaces;

namespace ScrumPokerAPI.Local;

public class LocalWebSocketClient(ConcurrentDictionary<string, WebSocket> sockets) : IWebSocketClient
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = sockets;

	public async Task SendMessageAsync(string connectionId, string message)
	{
		if (_sockets.TryGetValue(connectionId, out var socket))
		{
			if (socket.State != WebSocketState.Open)
				return;

			try
			{
				var buffer = Encoding.UTF8.GetBytes(message);

				await socket.SendAsync(
					buffer,
					WebSocketMessageType.Text,
					true,
					CancellationToken.None
				);
			}
			catch (WebSocketException)
			{
				// socket already closing/closed → ignore
			}
		}
	}
}