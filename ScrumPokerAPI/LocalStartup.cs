using System.Net.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Factories.ParticipantFactory;
using ScrumPokerAPI.Factories.ParticipantFactory.Interfaces;
using ScrumPokerAPI.Factories.RoomCodeAllocator;
using ScrumPokerAPI.Factories.RoomCodeAllocator.Interfaces;
using ScrumPokerAPI.Factories.RoomFactory;
using ScrumPokerAPI.Factories.RoomFactory.Interfaces;
using ScrumPokerAPI.Factories.RoomStateViewModelFactory;
using ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;
using ScrumPokerAPI.Repositories.RoomRepository;
using ScrumPokerAPI.Repositories.RoomRepository.Interfaces;
using ScrumPokerAPI.Services.BroadcastService;
using ScrumPokerAPI.Services.BroadcastService.Interfaces;
using ScrumPokerAPI.Services.LocalWebSocketHub;
using ScrumPokerAPI.Services.LocalWebSocketHub.Interfaces;
using ScrumPokerAPI.Services.RoomService;
using ScrumPokerAPI.Services.RoomService.Interfaces;
using ScrumPokerAPI.Services.WebSocketRequestHandler;

namespace ScrumPokerAPI;

public static class LocalStartup
{
    private const string LocalDevSection = "LocalDev";

    public static void ConfigureWebApplication(WebApplicationBuilder builder, ILocalWebSocketHub webSocketHub)
    {
        var configuration = builder.Configuration;
        var listenUrls = configuration[$"{LocalDevSection}:Urls"] ?? "http://localhost:5046";
        builder.WebHost.UseUrls(listenUrls);

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Set ConnectionStrings:DefaultConnection (env: ConnectionStrings__DefaultConnection) for local development.");

        builder.Services.AddSingleton(webSocketHub);
        builder.Services.AddDbContext<AppDatabaseContext>(options =>
            options.UseNpgsql(connectionString)
		);

        builder.Services.AddScoped<IRoomRepository, RoomRepository>();
        builder.Services.AddScoped<IRoomCodeAllocator, RoomCodeAllocator>();
        builder.Services.AddScoped<IRoomFactory, RoomFactory>();
        builder.Services.AddScoped<IParticipantFactory, ParticipantFactory>();
        builder.Services.AddSingleton<IRoomStateViewModelFactory, RoomStateViewModelFactory>();
        builder.Services.AddScoped<IRoomService, RoomService>();
        builder.Services.AddSingleton<IBroadcastService, LocalBroadcastService>();
        builder.Services.AddScoped<WebSocketRequestHandler>();
        builder.Services.Configure<HostOptions>(options =>
        {
            options.ShutdownTimeout = TimeSpan.FromSeconds(5);
        });
    }

    public static void MapLocalWebSocket(WebApplication application, LocalWebSocketHub webSocketHub)
    {
        var configuration = application.Configuration;
        var section = configuration.GetSection(LocalDevSection);
        var path = section["WebSocketPath"];
        if (string.IsNullOrEmpty(path))
            path = "/ws";

        var mockDomain = section["MockApiGatewayDomainName"] ?? "localhost:5046";
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
            var lifetime = httpContext.RequestServices.GetRequiredService<IHostApplicationLifetime>();
            using var shutdownLinked = CancellationTokenSource.CreateLinkedTokenSource(
                httpContext.RequestAborted,
                lifetime.ApplicationStopping
			);

            var connectionLifetime = shutdownLinked.Token;

            var webSocket = await httpContext.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
            var connectionId = Guid.NewGuid().ToString("N");
            webSocketHub.Register(connectionId, webSocket);

            var connectEvent = LocalApiGatewayRequestBuilder.Create(
                connectionId,
				"$connect",
				null,
				mockDomain,
				mockStage
			);

            await using (var scope = scopeFactory.CreateAsyncScope())
            {
                var webSocketRequestHandler = scope.ServiceProvider.GetRequiredService<WebSocketRequestHandler>();
                await webSocketRequestHandler.ProcessRequest(connectEvent, connectionLifetime).ConfigureAwait(false);
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
                        try
                        {
                            receiveResult = await webSocket
                                .ReceiveAsync(new ArraySegment<byte>(buffer), connectionLifetime)
                                .ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }

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
                        await webSocketRequestHandler.ProcessRequest(messageEvent, connectionLifetime).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                webSocketHub.Remove(connectionId);
                var disconnectEvent = LocalApiGatewayRequestBuilder.Create(
                    connectionId, "$disconnect", null, mockDomain, mockStage);
                await using (var scope = scopeFactory.CreateAsyncScope())
                {
                    var webSocketRequestHandler = scope.ServiceProvider.GetRequiredService<WebSocketRequestHandler>();
                    await webSocketRequestHandler.ProcessRequest(disconnectEvent, connectionLifetime).ConfigureAwait(false);
                }
            }
        });
    }
}
