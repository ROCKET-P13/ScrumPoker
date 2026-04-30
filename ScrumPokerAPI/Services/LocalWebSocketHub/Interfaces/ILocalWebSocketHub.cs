using System.Net.WebSockets;

namespace ScrumPokerAPI.Services.LocalWebSocketHub.Interfaces;

public interface ILocalWebSocketHub
{
	public void Register(string connectionid, WebSocket webSocket);
	public void Remove(string connectionId);
	public Task SendTextAsync(string connectionId,  ReadOnlyMemory<byte> utf8Payload, CancellationToken cancellationToken);
}