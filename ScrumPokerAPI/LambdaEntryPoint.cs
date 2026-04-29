using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using ScrumPokerAPI.Services.WebSocketRequestHandler;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace ScrumPokerAPI;

public class LambdaEntryPoint
{
    public async Task<APIGatewayProxyResponse> FunctionHandler(
        APIGatewayProxyRequest request,
        ILambdaContext lambdaContext,
        CancellationToken cancellationToken = default)
    {
        var serviceProvider = await LambdaStartup.GetOrCreateServiceProviderAsync(cancellationToken).ConfigureAwait(false);
        await using var scope = serviceProvider.CreateAsyncScope();
        var webSocketRequestHandler = scope.ServiceProvider.GetRequiredService<WebSocketRequestHandler>();
        return await webSocketRequestHandler.HandleAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
