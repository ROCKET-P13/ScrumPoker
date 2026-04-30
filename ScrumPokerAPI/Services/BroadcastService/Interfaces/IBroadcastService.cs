using Amazon.Lambda.APIGatewayEvents;
using ScrumPokerAPI.Models;

namespace ScrumPokerAPI.Services.BroadcastService.Interfaces;

public interface IBroadcastService
{
    Task BroadcastRoomState(
        APIGatewayProxyRequest request,
        IReadOnlyList<string> connectionIds,
        RoomStateViewModel state,
        CancellationToken cancellationToken);

    Task SendToConnectionAsync(
        APIGatewayProxyRequest request,
        string connectionId,
        object payload,
        CancellationToken cancellationToken);
}
