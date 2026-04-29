using ScrumPokerAPI.Services.WebSocketHub;

namespace ScrumPokerAPI;

public static class LocalEntryPoint
{
    public static async Task Main(string[] args)
    {
        var applicationBuilder = WebApplication.CreateBuilder(args);
        var localWebSocketHub = new LocalWebSocketHub();
        LocalStartup.ConfigureWebApplication(applicationBuilder, localWebSocketHub);

        var application = applicationBuilder.Build();
        application.UseWebSockets();
        LocalStartup.MapLocalWebSocket(application, localWebSocketHub);

        await application.RunAsync().ConfigureAwait(false);
    }
}
