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
            var roomId = await _roomService.RemoveConnectionAsync(connectionId, CancellationToken.None).ConfigureAwait(false);
            if (roomId == null)
                return EmptySuccessResponse();

            var targets = await _roomService.GetConnectionIdsForRoomAsync(roomId.Value, CancellationToken.None).ConfigureAwait(false);
            if (targets.Count == 0)
                return EmptySuccessResponse();

            var state = await _roomService.GetRoomStateAsync(roomId.Value, CancellationToken.None).ConfigureAwait(false);
            if (state != null)
                await _broadcastService.BroadcastRoomState(request, targets, state, CancellationToken.None).ConfigureAwait(false);

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

        ClientMessage? clientMessage;
        try
        {
            clientMessage = JsonSerializer.Deserialize<ClientMessage>(
                request.Body ?? "{}",
                AppJsonSerializerOptions.ApplicationDefault);
        }
        catch (JsonException)
        {
            await SendErrorAsync(request, connectionId, "Invalid JSON body.", cancellationToken).ConfigureAwait(false);
            return EmptySuccessResponse();
        }

        if (clientMessage == null || string.IsNullOrWhiteSpace(clientMessage.Action))
        {
            await SendErrorAsync(request, connectionId, "Missing action.", cancellationToken).ConfigureAwait(false);
            return EmptySuccessResponse();
        }

        var action = clientMessage.Action.Trim();

        try
        {
            switch (action)
            {
                case WebSocketRequestTypes.CreateRoom:
                    return await HandleCreateRoomRequest(request, connectionId, clientMessage, cancellationToken).ConfigureAwait(false);
                case WebSocketRequestTypes.JoinRoom:
                    return await HandleJoinRoomRequest(request, connectionId, clientMessage, cancellationToken).ConfigureAwait(false);
                case WebSocketRequestTypes.SendVote:
                    return await HandleSendVoteRequest(request, connectionId, clientMessage, cancellationToken).ConfigureAwait(false);
                case WebSocketRequestTypes.RevealVotes:
                    return await HandleRevealVotesRequest(request, connectionId, cancellationToken).ConfigureAwait(false);
                case WebSocketRequestTypes.ResetRound:
                    return await HandleResetRoundRequest(request, connectionId, cancellationToken).ConfigureAwait(false);
                default:
                    await SendErrorAsync(request, connectionId, $"Unknown action: {clientMessage.Action}.", cancellationToken)
                        .ConfigureAwait(false);
                    return EmptySuccessResponse();
            }
        }
        catch (Exception exception)
        {
            await SendErrorAsync(request, connectionId, exception.Message, cancellationToken).ConfigureAwait(false);
            return EmptySuccessResponse();
        }
    }

    private async Task<APIGatewayProxyResponse> HandleCreateRoomRequest(
        APIGatewayProxyRequest request,
        string connectionId,
        ClientMessage clientMessage,
        CancellationToken cancellationToken)
    {
        var trimmedDisplayName = clientMessage.DisplayName?.Trim() ?? string.Empty;
        if (trimmedDisplayName.Length == 0)
        {
            await SendErrorAsync(request, connectionId, "displayName is required.", cancellationToken).ConfigureAwait(false);
            return EmptySuccessResponse();
        }

        var state = await _roomService.CreateRoomAsync(
            connectionId,
            new CreateRoomRequestDto { DisplayName = trimmedDisplayName },
            cancellationToken).ConfigureAwait(false);
        var roomId = await _roomService.GetRoomIdForConnectionAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (roomId == null)
        {
            await SendErrorAsync(request, connectionId, "Could not resolve room after create.", cancellationToken)
                .ConfigureAwait(false);
            return EmptySuccessResponse();
        }

        var targets = await _roomService.GetConnectionIdsForRoomAsync(roomId.Value, cancellationToken).ConfigureAwait(false);
        await _broadcastService.BroadcastRoomState(request, targets, state, cancellationToken).ConfigureAwait(false);

        return EmptySuccessResponse();
    }

    private async Task<APIGatewayProxyResponse> HandleJoinRoomRequest(
        APIGatewayProxyRequest request,
        string connectionId,
        ClientMessage clientMessage,
        CancellationToken cancellationToken)
    {
        var normalizedRoomCode = clientMessage.RoomCode?.Trim() ?? string.Empty;
        var trimmedDisplayName = clientMessage.DisplayName?.Trim() ?? string.Empty;
        if (normalizedRoomCode.Length == 0 || trimmedDisplayName.Length == 0)
        {
            await SendErrorAsync(request, connectionId, "roomCode and displayName are required.", cancellationToken)
                .ConfigureAwait(false);
            return EmptySuccessResponse();
        }

        var state = await _roomService.JoinRoomAsync(
            connectionId,
            new JoinRoomRequestDto
			{
				RoomCode = normalizedRoomCode,
				DisplayName = trimmedDisplayName
			},
            cancellationToken
			)
			.ConfigureAwait(false);

        if (state == null)
        {
            await SendErrorAsync(request, connectionId, "Room not found.", cancellationToken).ConfigureAwait(false);
            return EmptySuccessResponse();
        }

        var roomId = await _roomService.GetRoomIdForConnectionAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (roomId == null)
		{
            return EmptySuccessResponse();
		}

        var targets = await _roomService.GetConnectionIdsForRoomAsync(roomId.Value, cancellationToken).ConfigureAwait(false);
        await _broadcastService.BroadcastRoomState(request, targets, state, cancellationToken).ConfigureAwait(false);

        return EmptySuccessResponse();
    }

    private async Task<APIGatewayProxyResponse> HandleSendVoteRequest(
        APIGatewayProxyRequest request,
        string connectionId,
        ClientMessage clientMessage,
        CancellationToken cancellationToken)
    {
        var trimmedVoteValue = clientMessage.Value?.Trim() ?? string.Empty;
        if (trimmedVoteValue.Length == 0)
        {
            await SendErrorAsync(request, connectionId, "value is required.", cancellationToken).ConfigureAwait(false);
            return EmptySuccessResponse();
        }

        var state = await _roomService.VoteAsync(
            connectionId,
            new VoteRequestDto { Value = trimmedVoteValue },
            cancellationToken).ConfigureAwait(false);
        if (state == null)
        {
            await SendErrorAsync(request, connectionId, "Not in a room.", cancellationToken).ConfigureAwait(false);
            return EmptySuccessResponse();
        }

        var roomId = await _roomService.GetRoomIdForConnectionAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (roomId == null)
            return EmptySuccessResponse();

        var targets = await _roomService.GetConnectionIdsForRoomAsync(roomId.Value, cancellationToken).ConfigureAwait(false);
        await _broadcastService.BroadcastRoomState(request, targets, state, cancellationToken).ConfigureAwait(false);

        return EmptySuccessResponse();
    }

    private async Task<APIGatewayProxyResponse> HandleRevealVotesRequest(APIGatewayProxyRequest request, string connectionId, CancellationToken cancellationToken)
    {
        var state = await _roomService.RevealAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (state == null)
        {
            await SendErrorAsync(request, connectionId, "Not in a room.", cancellationToken).ConfigureAwait(false);
            return EmptySuccessResponse();
        }

        var roomId = await _roomService.GetRoomIdForConnectionAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (roomId == null)
            return EmptySuccessResponse();

        var targets = await _roomService.GetConnectionIdsForRoomAsync(roomId.Value, cancellationToken).ConfigureAwait(false);
        await _broadcastService.BroadcastRoomState(request, targets, state, cancellationToken).ConfigureAwait(false);

        return EmptySuccessResponse();
    }

    private async Task<APIGatewayProxyResponse> HandleResetRoundRequest(APIGatewayProxyRequest request, string connectionId, CancellationToken cancellationToken)
    {
        var state = await _roomService.ResetRoundAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (state == null)
        {
            await SendErrorAsync(request, connectionId, "Not in a room.", cancellationToken).ConfigureAwait(false);
            return EmptySuccessResponse();
        }

        var roomId = await _roomService.GetRoomIdForConnectionAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (roomId == null)
            return EmptySuccessResponse();

        var targets = await _roomService.GetConnectionIdsForRoomAsync(roomId.Value, cancellationToken).ConfigureAwait(false);
        await _broadcastService.BroadcastRoomState(request, targets, state, cancellationToken).ConfigureAwait(false);

        return EmptySuccessResponse();
    }

    private Task SendErrorAsync(
        APIGatewayProxyRequest request,
        string connectionId,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        return _broadcastService.SendToConnectionAsync(
			request,
			connectionId,
			new
			{
				type = "error",
				message = errorMessage
			},
			cancellationToken
		);
    }

    private static APIGatewayProxyResponse EmptySuccessResponse() =>
        new() { StatusCode = 200, Body = "{}" };
}
