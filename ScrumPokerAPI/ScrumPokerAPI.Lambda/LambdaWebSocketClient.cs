using System.Text;
using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using ScrumPokerAPI.Core.Interfaces;

namespace ScrumPokerAPI.Lambda;

public class LambdaWebSocketClient(string endpoint) : IWebSocketClient
{
	private readonly IAmazonApiGatewayManagementApi _client = new AmazonApiGatewayManagementApiClient(
		new AmazonApiGatewayManagementApiConfig
		{
			ServiceURL = endpoint
		}
	);

	public async Task SendMessageAsync(string connectionId, string message)
	{
		var bytes = Encoding.UTF8.GetBytes(message);

		using var stream = new MemoryStream(bytes);

		await _client.PostToConnectionAsync(new PostToConnectionRequest
		{
			ConnectionId = connectionId,
			Data = stream
		});
	}

}