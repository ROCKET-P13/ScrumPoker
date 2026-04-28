using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using ScrumPokerAPI.Core.Models;
using ScrumPokerAPI.Core.Services;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ScrumPokerAPI.Lambda;

public class Function
{
    private readonly MessageRouter _router;

    public Function()
    {
        var endpoint = Environment.GetEnvironmentVariable("WEBSOCKET_ENDPOINT");

        if (string.IsNullOrEmpty(endpoint))
            throw new Exception("WEBSOCKET_ENDPOINT is not set");

        var wsClient = new LambdaWebSocketClient(endpoint);

        _router = new MessageRouter(wsClient);
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

        await _router.Route(socketRequest);

        return new APIGatewayProxyResponse
        {
            StatusCode = 200
        };
    }
}