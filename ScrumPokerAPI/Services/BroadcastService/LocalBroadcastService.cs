using System.Text;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using ScrumPokerAPI.Models;
using ScrumPokerAPI.Serialization;
using ScrumPokerAPI.Services.BroadcastService.Interfaces;
using ScrumPokerAPI.Services.LocalWebSocketHub.Interfaces;

namespace ScrumPokerAPI.Services.BroadcastService;

public sealed class LocalBroadcastService(ILocalWebSocketHub webSocketHub) : IBroadcastService
{
	private readonly ILocalWebSocketHub _webSocketHub =  webSocketHub;
    public async Task BroadcastRoomState(
        APIGatewayProxyRequest request,
        IReadOnlyList<string> connectionIds,
        RoomStateViewModel state,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            type = WebSocketEnvelopeType.Event,
            @event = WebSocketEventNames.RoomState,
            payload = state,
        };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, AppJsonSerializerOptions.ApplicationDefault)).AsMemory();

        foreach (var connectionId in connectionIds)
            await _webSocketHub.SendTextAsync(connectionId, bytes, cancellationToken).ConfigureAwait(false);
    }

    public async Task SendToConnectionAsync(
        APIGatewayProxyRequest request,
        string connectionId,
        object payload,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, AppJsonSerializerOptions.ApplicationDefault)).AsMemory();
        await _webSocketHub.SendTextAsync(connectionId, bytes, cancellationToken).ConfigureAwait(false);
    }
}
