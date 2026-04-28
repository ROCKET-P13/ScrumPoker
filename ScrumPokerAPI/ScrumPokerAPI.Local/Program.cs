using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using ScrumPokerAPI.Core.Models;
using ScrumPokerAPI.Core.Services;
using ScrumPokerAPI.Local;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

var sockets = new ConcurrentDictionary<string, WebSocket>();

var roomService = new RoomService();
var shutdownCts = new CancellationTokenSource();

app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Closing all sockets...");

    shutdownCts.Cancel();

    foreach (var socket in sockets.Values)
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
        catch (Exception ex)
        {
            Console.WriteLine($"Socket close error: {ex.Message}");
        }
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

    var webSocketClient = new LocalWebSocketClient(sockets);
    var dispatcher = new MessageDispatcher(webSocketClient, roomService);

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

        try
        {
            if (socket.State == WebSocketState.Open)
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