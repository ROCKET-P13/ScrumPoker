using System.Collections.Concurrent;
using System.Net.WebSockets;
using ScrumPokerAPI.Services.LocalWebSocketHub.Interfaces;

namespace ScrumPokerAPI.Services.LocalWebSocketHub;

public sealed class LocalWebSocketHub : ILocalWebSocketHub
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public void Register(string connectionId, WebSocket webSocket)
    {
        _connections[connectionId] = webSocket;
    }

    public void Remove(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public async Task SendTextAsync(string connectionId, ReadOnlyMemory<byte> utf8Payload, CancellationToken cancellationToken)
    {
        if (!_connections.TryGetValue(connectionId, out var webSocket))
            return;
        if (webSocket.State != WebSocketState.Open)
            return;

        await webSocket.SendAsync(utf8Payload, WebSocketMessageType.Text, endOfMessage: true, cancellationToken).ConfigureAwait(false);
    }
}
