using Amazon.Lambda.APIGatewayEvents;

namespace ScrumPokerAPI;

public static class LocalApiGatewayRequestBuilder
{
    public static APIGatewayProxyRequest Create(
        string connectionId,
        string routeKey,
        string? body,
        string mockApiGatewayDomainName,
        string mockApiGatewayStage
	)
    {
        return new APIGatewayProxyRequest
        {
            Body = body,
            RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
            {
                ConnectionId = connectionId,
                RouteKey = routeKey,
                DomainName = mockApiGatewayDomainName,
                Stage = mockApiGatewayStage,
                ApiId = "local",
            },
        };
    }
}
