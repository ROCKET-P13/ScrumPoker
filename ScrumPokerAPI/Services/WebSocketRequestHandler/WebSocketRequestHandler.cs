using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using ScrumPokerAPI.Models;
using ScrumPokerAPI.Models.Requests;
using ScrumPokerAPI.Serialization;
using ScrumPokerAPI.Services.BroadcastService.Interfaces;
using ScrumPokerAPI.Services.RoomService.Interfaces;

namespace ScrumPokerAPI.Services.WebSocketRequestHandler;

public sealed class WebSocketRequestHandler(IRoomService roomService, IBroadcastService broadcastService)
{
	private readonly IRoomService _roomService = roomService;
	private readonly IBroadcastService _broadcastService = broadcastService;

	public async Task<APIGatewayProxyResponse> ProcessRequest(APIGatewayProxyRequest request, CancellationToken cancellationToken)
	{
		var route = request.RequestContext.RouteKey ?? string.Empty;
		return route switch
		{
			"$connect" => await HandleConnectAsync().ConfigureAwait(false),
			"$disconnect" => await HandleDisconnectAsync(request, cancellationToken).ConfigureAwait(false),
			"$default" => await HandleDefaultAsync(request, cancellationToken).ConfigureAwait(false),
			_ => EmptySuccessResponse(),
		};
	}

	private static Task<APIGatewayProxyResponse> HandleConnectAsync()
	{
		return Task.FromResult(EmptySuccessResponse());
	}

	private async Task<APIGatewayProxyResponse> HandleDisconnectAsync(APIGatewayProxyRequest request, CancellationToken _)
	{
		var connectionId = request.RequestContext.ConnectionId;
		if (string.IsNullOrEmpty(connectionId))
			return EmptySuccessResponse();

		try
		{
			var roomId = await _roomService.RemoveConnection(connectionId, CancellationToken.None).ConfigureAwait(false);
			if (roomId == null)
				return EmptySuccessResponse();

			var targets = await _roomService.GetConnectionIdsForRoom(roomId.Value, CancellationToken.None).ConfigureAwait(false);
			if (targets.Count == 0)
				return EmptySuccessResponse();

			var roomState = await _roomService.GetRoomState(roomId.Value, CancellationToken.None).ConfigureAwait(false);
			if (roomState != null)
				await _broadcastService.BroadcastRoomState(request, targets, roomState, CancellationToken.None).ConfigureAwait(false);

			return EmptySuccessResponse();
		}
		catch (OperationCanceledException)
		{
			return EmptySuccessResponse();
		}
	}

	private async Task<APIGatewayProxyResponse> HandleDefaultAsync(APIGatewayProxyRequest request, CancellationToken cancellationToken)
	{
		var connectionId = request.RequestContext.ConnectionId;
		if (string.IsNullOrEmpty(connectionId))
			return EmptySuccessResponse();

		ClientWebSocketCommandMessage? commandMessage;
		try
		{
			commandMessage = JsonSerializer.Deserialize<ClientWebSocketCommandMessage>(
				request.Body ?? "{}",
				AppJsonSerializerOptions.ApplicationDefault);
		}
		catch (JsonException)
		{
			await SendResponseEnvelopeAsync(request, connectionId, null, false, new { message = "Invalid JSON body." }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		if (commandMessage == null)
		{
			await SendResponseEnvelopeAsync(request, connectionId, null, false, new { message = "Missing message body." }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var requestId = commandMessage.RequestId;
		if (!string.Equals(commandMessage.Type, WebSocketEnvelopeType.Command, StringComparison.OrdinalIgnoreCase))
		{
			await SendResponseEnvelopeAsync(
				request,
				connectionId,
				requestId,
				false,
				new { message = $"Expected type \"{WebSocketEnvelopeType.Command}\"." },
				cancellationToken).ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		if (string.IsNullOrWhiteSpace(commandMessage.Action))
		{
			await SendResponseEnvelopeAsync(request, connectionId, requestId, false, new { message = "Missing action." }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var action = commandMessage.Action.Trim();
		var payloadElement = ResolvePayloadElement(commandMessage);

		try
		{
			switch (action)
			{
				case WebSocketRequestTypes.CreateRoom:
					return await HandleCreateRoomRequest(request, connectionId, requestId, payloadElement, cancellationToken)
						.ConfigureAwait(false);
				case WebSocketRequestTypes.JoinRoom:
					return await HandleJoinRoomRequest(request, connectionId, requestId, payloadElement, cancellationToken)
						.ConfigureAwait(false);
				case WebSocketRequestTypes.SendVote:
					return await HandleSendVoteRequest(request, connectionId, requestId, payloadElement, cancellationToken)
						.ConfigureAwait(false);
				case WebSocketRequestTypes.RevealVotes:
					return await HandleRevealVotesRequest(request, connectionId, requestId, cancellationToken).ConfigureAwait(false);
				case WebSocketRequestTypes.ResetRound:
					return await HandleResetRoundRequest(request, connectionId, requestId, cancellationToken).ConfigureAwait(false);
				default:
					await SendResponseEnvelopeAsync(request, connectionId, requestId, false, new { message = $"Unknown action: {action}." }, cancellationToken)
						.ConfigureAwait(false);
					return EmptySuccessResponse();
			}
		}
		catch (Exception exception)
		{
			await SendResponseEnvelopeAsync(request, connectionId, requestId, false, new { message = exception.Message }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}
	}

	private async Task<APIGatewayProxyResponse> HandleCreateRoomRequest(
		APIGatewayProxyRequest request,
		string connectionId,
		string? requestId,
		JsonElement payloadElement,
		CancellationToken cancellationToken
	)
	{
		CreateRoomCommandPayload? payload;
		try
		{
			payload = JsonSerializer.Deserialize<CreateRoomCommandPayload>(
				payloadElement.GetRawText(),
				AppJsonSerializerOptions.ApplicationDefault);
		}
		catch (JsonException)
		{
			await SendResponseEnvelopeAsync(
				request,
				connectionId,
				requestId,
				false,
				new { message = "Invalid payload." },
				cancellationToken
			).ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var trimmedDisplayName = payload?.DisplayName?.Trim() ?? string.Empty;
		if (trimmedDisplayName.Length == 0)
		{
			await SendResponseEnvelopeAsync(
				request,
				connectionId,
				requestId,
				false,
				new { message = "displayName is required." },
				cancellationToken
			).ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var roomState = await _roomService.CreateRoomAsync(
				connectionId,
				new CreateRoomRequestDTO { DisplayName = trimmedDisplayName },
				cancellationToken)
			.ConfigureAwait(false);

		var roomId = await _roomService.GetRoomIdForConnection(connectionId, cancellationToken).ConfigureAwait(false);
		if (roomId == null)
		{
			await SendResponseEnvelopeAsync(
				request,
				connectionId,
				requestId,
				false,
				new { message = "Could not resolve room after create." },
				cancellationToken
			).ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var targets = await _roomService.GetConnectionIdsForRoom(roomId.Value, cancellationToken).ConfigureAwait(false);

		await SendResponseEnvelopeAsync(
			request,
			connectionId,
			requestId,
			true,
			roomState,
			cancellationToken
		).ConfigureAwait(false);
		await _broadcastService.BroadcastRoomState(request, targets, roomState, cancellationToken).ConfigureAwait(false);
		return EmptySuccessResponse();
	}

	private async Task<APIGatewayProxyResponse> HandleJoinRoomRequest(
		APIGatewayProxyRequest request,
		string connectionId,
		string? requestId,
		JsonElement payloadElement,
		CancellationToken cancellationToken
	)
	{
		JoinRoomCommandPayload? payload;
		try
		{
			payload = JsonSerializer.Deserialize<JoinRoomCommandPayload>(
				payloadElement.GetRawText(),
				AppJsonSerializerOptions.ApplicationDefault);
		}
		catch (JsonException)
		{
			await SendResponseEnvelopeAsync(request, connectionId, requestId, false, new { message = "Invalid payload." }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var normalizedRoomCode = payload?.RoomCode?.Trim() ?? string.Empty;
		var trimmedDisplayName = payload?.DisplayName?.Trim() ?? string.Empty;
		if (normalizedRoomCode.Length == 0 || trimmedDisplayName.Length == 0)
		{
			await SendResponseEnvelopeAsync(
				request,
				connectionId,
				requestId,
				false,
				new { message = "roomCode and displayName are required." },
				cancellationToken).ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var roomState = await _roomService.JoinRoom(
				connectionId,
				new JoinRoomRequestDTO
				{
					RoomCode = normalizedRoomCode,
					DisplayName = trimmedDisplayName
				},
				cancellationToken)
			.ConfigureAwait(false);

		if (roomState == null)
		{
			await SendResponseEnvelopeAsync(request, connectionId, requestId, false, new { message = "Room not found." }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var roomId = await _roomService.GetRoomIdForConnection(connectionId, cancellationToken).ConfigureAwait(false);
		if (roomId == null)
			return EmptySuccessResponse();

		var targets = await _roomService.GetConnectionIdsForRoom(roomId.Value, cancellationToken).ConfigureAwait(false);

		await SendResponseEnvelopeAsync(request, connectionId, requestId, true, new { }, cancellationToken).ConfigureAwait(false);
		await _broadcastService.BroadcastRoomState(request, targets, roomState, cancellationToken).ConfigureAwait(false);

		return EmptySuccessResponse();
	}

	private async Task<APIGatewayProxyResponse> HandleSendVoteRequest(
		APIGatewayProxyRequest request,
		string connectionId,
		string? requestId,
		JsonElement payloadElement,
		CancellationToken cancellationToken
	)
	{
		SendVoteCommandPayload? payload;
		try
		{
			payload = JsonSerializer.Deserialize<SendVoteCommandPayload>(
				payloadElement.GetRawText(),
				AppJsonSerializerOptions.ApplicationDefault);
		}
		catch (JsonException)
		{
			await SendResponseEnvelopeAsync(request, connectionId, requestId, false, new { message = "Invalid payload." }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var trimmedVoteValue = payload?.Value?.Trim() ?? string.Empty;
		if (trimmedVoteValue.Length == 0)
		{
			await SendResponseEnvelopeAsync(request, connectionId, requestId, false, new { message = "value is required." }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var roomState = await _roomService.CaptureVote(
				connectionId,
				new VoteRequestDTO { Value = trimmedVoteValue },
				cancellationToken)
			.ConfigureAwait(false);

		if (roomState == null)
		{
			await SendResponseEnvelopeAsync(request, connectionId, requestId, false, new { message = "Not in a room." }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var roomId = await _roomService.GetRoomIdForConnection(connectionId, cancellationToken).ConfigureAwait(false);
		if (roomId == null)
			return EmptySuccessResponse();

		var targets = await _roomService.GetConnectionIdsForRoom(roomId.Value, cancellationToken).ConfigureAwait(false);

		await SendResponseEnvelopeAsync(request, connectionId, requestId, true, new { }, cancellationToken).ConfigureAwait(false);
		await _broadcastService.BroadcastRoomState(request, targets, roomState, cancellationToken).ConfigureAwait(false);

		return EmptySuccessResponse();
	}

	private async Task<APIGatewayProxyResponse> HandleRevealVotesRequest(
		APIGatewayProxyRequest request,
		string connectionId,
		string? requestId,
		CancellationToken cancellationToken)
	{
		var roomState = await _roomService.RevealVotes(connectionId, cancellationToken).ConfigureAwait(false);
		if (roomState == null)
		{
			await SendResponseEnvelopeAsync(request, connectionId, requestId, false, new { message = "Not in a room." }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var roomId = await _roomService.GetRoomIdForConnection(connectionId, cancellationToken).ConfigureAwait(false);
		if (roomId == null)
			return EmptySuccessResponse();

		var targets = await _roomService.GetConnectionIdsForRoom(roomId.Value, cancellationToken).ConfigureAwait(false);

		await SendResponseEnvelopeAsync(request, connectionId, requestId, true, new { }, cancellationToken).ConfigureAwait(false);
		await _broadcastService.BroadcastRoomState(request, targets, roomState, cancellationToken).ConfigureAwait(false);

		return EmptySuccessResponse();
	}

	private async Task<APIGatewayProxyResponse> HandleResetRoundRequest(
		APIGatewayProxyRequest request,
		string connectionId,
		string? requestId,
		CancellationToken cancellationToken)
	{
		var roomState = await _roomService.ResetRound(connectionId, cancellationToken).ConfigureAwait(false);
		if (roomState == null)
		{
			await SendResponseEnvelopeAsync(request, connectionId, requestId, false, new { message = "Not in a room." }, cancellationToken)
				.ConfigureAwait(false);
			return EmptySuccessResponse();
		}

		var roomId = await _roomService.GetRoomIdForConnection(connectionId, cancellationToken).ConfigureAwait(false);
		if (roomId == null)
			return EmptySuccessResponse();

		var targets = await _roomService.GetConnectionIdsForRoom(roomId.Value, cancellationToken).ConfigureAwait(false);

		await SendResponseEnvelopeAsync(request, connectionId, requestId, true, new { }, cancellationToken).ConfigureAwait(false);
		await _broadcastService.BroadcastRoomState(request, targets, roomState, cancellationToken).ConfigureAwait(false);

		return EmptySuccessResponse();
	}

	private Task SendResponseEnvelopeAsync(
		APIGatewayProxyRequest request,
		string connectionId,
		string? requestId,
		bool success,
		object payload,
		CancellationToken cancellationToken
	)
	{
		return _broadcastService.SendToConnectionAsync(
			request,
			connectionId,
			new
			{
				type = WebSocketEnvelopeType.Response,
				requestId,
				success,
				payload,
			},
			cancellationToken);
	}

	private static JsonElement ResolvePayloadElement(ClientWebSocketCommandMessage message)
	{
		if (message.Payload is not JsonElement element)
			return JsonSerializer.SerializeToElement(new { }, AppJsonSerializerOptions.ApplicationDefault);

		return element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
			? JsonSerializer.SerializeToElement(new { }, AppJsonSerializerOptions.ApplicationDefault)
			: element;
	}

	private static APIGatewayProxyResponse EmptySuccessResponse() =>
		new() { StatusCode = 200, Body = "{}" };
}
