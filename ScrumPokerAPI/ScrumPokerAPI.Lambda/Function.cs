using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ScrumPokerAPI.Core.Handlers;
using ScrumPokerAPI.Core.Models;
using ScrumPokerAPI.Core.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ScrumPokerAPI.Lambda;

public class Function
{
    private readonly HandlerRegistry _dispatcher;

    public Function()
    {
        var endpoint = Environment.GetEnvironmentVariable("WEBSOCKET_ENDPOINT");

        var webSocketClient = new LambdaWebSocketClient(endpoint!);
		var roomService = new RoomService();
		var joinHandler = new JoinRoomHandler(webSocketClient, roomService);

        _dispatcher = new HandlerRegistry(joinHandler);
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext context)
    {
        var socketRequest = new SocketRequest
        {
            ConnectionId = request.RequestContext.ConnectionId,
            RouteKey = request.RequestContext.RouteKey,
            Body = request.Body
        };

        await _dispatcher.Dispatch(socketRequest);

        return new APIGatewayProxyResponse
        {
            StatusCode = 200
        };
    }
}