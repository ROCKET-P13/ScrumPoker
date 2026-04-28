using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using ScrumPokerAPI.Core.Handlers;
using ScrumPokerAPI.Core.Models;
using ScrumPokerAPI.Core.Services;
using ScrumPokerAPI.Local;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

var sockets = new ConcurrentDictionary<string, WebSocket>();

var roomService = new RoomService();
var shutdownCts = new CancellationTokenSource();

var webSocketClient = new LocalWebSocketClient(sockets);
var joinHandler = new JoinRoomHandler(webSocketClient, roomService);
var voteHandler = new VoteHandler(webSocketClient, roomService);
var dispatcher = new HandlerRegistry(joinHandler, voteHandler);

app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Closing all sockets...");

    shutdownCts.Cancel();

    foreach (var (id, socket) in sockets)
    {
		try
		{
			if (socket.State == WebSocketState.Open ||
				socket.State == WebSocketState.CloseReceived)
			{
				socket.CloseAsync(
					WebSocketCloseStatus.NormalClosure,
					"Server shutting down",
					CancellationToken.None
				).GetAwaiter().GetResult();
			}

			socket.Dispose();
		}
		catch { }
    }

    sockets.Clear();
});

app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var socket = await context.WebSockets.AcceptWebSocketAsync();
    var connectionId = Guid.NewGuid().ToString();

    sockets[connectionId] = socket;

    var buffer = new byte[4096];

    using var shutdownContext = new CancellationTokenSource();

    using var linkedContext = CancellationTokenSource.CreateLinkedTokenSource(
        context.RequestAborted,
        shutdownContext.Token
    );

    var ct = linkedContext.Token;

    try
    {
        while (!ct.IsCancellationRequested &&
               socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, ct);

            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

            var request = new SocketRequest
            {
                ConnectionId = connectionId,
                RouteKey = "MESSAGE",
                Body = message
            };

            await dispatcher.Dispatch(request);
        }
    }
    catch (OperationCanceledException)
    {
        // expected during shutdown
    }
    catch (WebSocketException)
    {
        // client disconnects
    }
    finally
    {
        sockets.TryRemove(connectionId, out _);

        var updatedRoom = roomService.RemovePlayer(connectionId);

        if (updatedRoom != null)
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "ROOM_STATE",
                room = updatedRoom
            });

            foreach (var player in updatedRoom.Players)
            {
                await webSocketClient.SendMessageAsync(player.ConnectionId, payload);
            }
        }

        try
        {
            if (socket.State == WebSocketState.Open ||
                socket.State == WebSocketState.CloseReceived)
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Closing",
                    CancellationToken.None);
            }
        }
        catch { }

        socket.Dispose();
    }
});

app.Run();