using System.Net.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Data.Repositories;
using ScrumPokerAPI.Data.Services;
using ScrumPokerAPI.Domain.Factories;
using ScrumPokerAPI.Domain.Repositories;
using ScrumPokerAPI.Domain.Services;
using ScrumPokerAPI.Services;
using ScrumPokerAPI.ViewModels.Factories;

namespace ScrumPokerAPI;

/// <summary>
/// Local Kestrel host. Values under configuration section <c>LocalDev</c> (appsettings, env, user secrets).
/// Environment overrides: <c>LocalDev__Urls</c>, <c>LocalDev__MockApiGatewayDomainName</c>,
/// <c>LocalDev__MockApiGatewayStage</c>, <c>LocalDev__WebSocketPath</c>.
/// </summary>
public static class LocalStartup
{
    private const string LocalDevSection = "LocalDev";

    public static void ConfigureWebApplication(WebApplicationBuilder builder, LocalWebSocketHub hub)
    {
        var configuration = builder.Configuration;
        var listenUrls = configuration[$"{LocalDevSection}:Urls"] ?? "http://localhost:5007";
        builder.WebHost.UseUrls(listenUrls);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings:DefaultConnection (env: ConnectionStrings__DefaultConnection) for local development.");

        builder.Services.AddSingleton(hub);
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        builder.Services.AddScoped<IRoomRepository, RoomRepository>();
        builder.Services.AddScoped<IRoomCodeAllocator, RoomCodeAllocator>();
        builder.Services.AddScoped<IRoomFactory, RoomFactory>();
        builder.Services.AddScoped<IParticipantFactory, ParticipantFactory>();
        builder.Services.AddSingleton<IRoomStateViewModelFactory, RoomStateViewModelFactory>();
        builder.Services.AddScoped<RoomService>();
        builder.Services.AddSingleton<IBroadcastService, LocalBroadcastService>();
        builder.Services.AddScoped<WebSocketRequestHandler>();
    }

    public static void MapLocalWebSocket(WebApplication application, LocalWebSocketHub hub)
    {
        var configuration = application.Configuration;
        var section = configuration.GetSection(LocalDevSection);
        var path = section["WebSocketPath"];
        if (string.IsNullOrEmpty(path))
            path = "/ws";

        var mockDomain = section["MockApiGatewayDomainName"] ?? "localhost:5007";
        var mockStage = section["MockApiGatewayStage"] ?? "local";

        application.MapGet("/", () => Results.Text(
            $"Scrum Poker API (local). WebSocket: ws://<host>{path}",
            "text/plain"));

        application.Map(path, async (HttpContext httpContext) =>
        {
            if (!httpContext.WebSockets.IsWebSocketRequest)
            {
                httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var scopeFactory = httpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();
            var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var connectionId = Guid.NewGuid().ToString("N");
            hub.Register(connectionId, webSocket);

            var connectEvent = LocalApiGatewayRequestBuilder.Create(
                connectionId, "$connect", null, mockDomain, mockStage);
            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                var webSocketRequestHandler = scope.ServiceProvider.GetRequiredService<WebSocketRequestHandler>();
                await webSocketRequestHandler.HandleAsync(connectEvent, httpContext.RequestAborted).ConfigureAwait(false);
            }

            try
            {
                var buffer = new byte[16 * 1024];
                while (webSocket.State == WebSocketState.Open)
                {
                    var messageStream = new MemoryStream();
                    WebSocketReceiveResult receiveResult;
                    do
                    {
                        receiveResult = await webSocket
                            .ReceiveAsync(new ArraySegment<byte>(buffer), httpContext.RequestAborted)
                            .ConfigureAwait(false);

                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                            return;

                        messageStream.Write(buffer, 0, receiveResult.Count);
                    } while (!receiveResult.EndOfMessage);

                    var text = System.Text.Encoding.UTF8.GetString(messageStream.ToArray());
                    var messageEvent = LocalApiGatewayRequestBuilder.Create(
                        connectionId,
                        "$default",
                        text,
                        mockDomain,
                        mockStage);

                    await using (var scope = scopeFactory.CreateAsyncScope())
                    {
                        var webSocketRequestHandler = scope.ServiceProvider.GetRequiredService<WebSocketRequestHandler>();
                        await webSocketRequestHandler.HandleAsync(messageEvent, httpContext.RequestAborted).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                hub.Remove(connectionId);
                var disconnectEvent = LocalApiGatewayRequestBuilder.Create(
                    connectionId, "$disconnect", null, mockDomain, mockStage);
                await using (var scope = scopeFactory.CreateAsyncScope())
                {
                    var webSocketRequestHandler = scope.ServiceProvider.GetRequiredService<WebSocketRequestHandler>();
                    await webSocketRequestHandler.HandleAsync(disconnectEvent, httpContext.RequestAborted).ConfigureAwait(false);
                }
            }
        });
    }
}
