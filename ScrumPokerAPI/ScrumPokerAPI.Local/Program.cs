using System.Net.WebSockets;
using System.Text;
using ScrumPokerAPI.Core.Models;
using ScrumPokerAPI.Core.Services;
using ScrumPokerAPI.Local;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var sockets = new Dictionary<string, WebSocket>();

app.UseWebSockets();

app.Map("/ws", async context =>
{
    var socket = await context.WebSockets.AcceptWebSocketAsync();
    var connectionId = Guid.NewGuid().ToString();

    sockets[connectionId] = socket;

    var wsClient = new LocalWebSocketClient(sockets);
    var dispatcher = new MessageDispatcher(wsClient);

    var buffer = new byte[4096]; // increased buffer size (important for JSON)

    var ct = context.RequestAborted;

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
        // triggered when server shuts down
    }
    finally
    {
        sockets.Remove(connectionId);

        if (socket.State == WebSocketState.Open ||
            socket.State == WebSocketState.CloseReceived)
        {
            try
            {
                await socket.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "Server shutting down",
                    CancellationToken.None);
            }
            catch
            {
                // ignore shutdown race conditions
            }
        }

        socket.Dispose();
    }
});

app.Run();