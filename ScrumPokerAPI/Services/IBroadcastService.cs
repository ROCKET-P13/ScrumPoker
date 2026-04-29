using Amazon.Lambda.APIGatewayEvents;
using ScrumPokerAPI.Models;

namespace ScrumPokerAPI.Services;

public interface IBroadcastService
{
    Task BroadcastRoomStateAsync(
        APIGatewayProxyRequest request,
        IReadOnlyList<string> connectionIds,
        RoomStateDto state,
        CancellationToken cancellationToken);

    Task SendToConnectionAsync(
        APIGatewayProxyRequest request,
        string connectionId,
        object payload,
        CancellationToken cancellationToken);
}
