using System.Net;
using System.Text;
using System.Text.Json;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Runtime;
using ScrumPokerAPI.Models;
using ScrumPokerAPI.Serialization;

namespace ScrumPokerAPI.Services;

public sealed class ApiGatewayBroadcastService : IBroadcastService
{
    public async Task BroadcastRoomStateAsync(
        APIGatewayProxyRequest request,
        IReadOnlyList<string> connectionIds,
        RoomStateDto state,
        CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint(request);
        using var client = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = endpoint,
        });

        var payload = new
        {
            type = "roomState",
            data = state,
        };
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, AppJsonSerializerOptions.ApplicationDefault));

        foreach (var connectionId in connectionIds)
        {
            try
            {
                await client.PostToConnectionAsync(
                    new PostToConnectionRequest
                    {
                        ConnectionId = connectionId,
                        Data = new MemoryStream(bytes),
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonServiceException exception) when (exception.StatusCode == HttpStatusCode.Gone)
            {
            }
        }
    }

    public async Task SendToConnectionAsync(
        APIGatewayProxyRequest request,
        string connectionId,
        object payload,
        CancellationToken cancellationToken)
    {
        var endpoint = BuildEndpoint(request);
        using var client = new AmazonApiGatewayManagementApiClient(new AmazonApiGatewayManagementApiConfig
        {
            ServiceURL = endpoint,
        });

        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, AppJsonSerializerOptions.ApplicationDefault));
        await client.PostToConnectionAsync(
            new PostToConnectionRequest
            {
                ConnectionId = connectionId,
                Data = new MemoryStream(bytes),
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static string BuildEndpoint(APIGatewayProxyRequest request)
    {
        var domain = request.RequestContext.DomainName;
        var stage = request.RequestContext.Stage;
        if (string.IsNullOrEmpty(domain) || string.IsNullOrEmpty(stage))
            throw new InvalidOperationException("RequestContext.DomainName or Stage is missing.");

        return $"https://{domain}/{stage}";
    }
}
