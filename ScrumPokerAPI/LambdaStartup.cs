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

public static class LambdaStartup
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static ServiceProvider? _serviceProvider;

    public static void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IRoomCodeAllocator, RoomCodeAllocator>();
        services.AddScoped<IRoomFactory, RoomFactory>();
        services.AddScoped<IParticipantFactory, ParticipantFactory>();
        services.AddSingleton<IRoomStateViewModelFactory, RoomStateViewModelFactory>();
        services.AddScoped<RoomService>();
        services.AddSingleton<IBroadcastService, ApiGatewayBroadcastService>();
        services.AddScoped<WebSocketRequestHandler>();
    }

    public static async Task<ServiceProvider> GetOrCreateServiceProviderAsync(CancellationToken cancellationToken)
    {
        if (_serviceProvider != null)
            return _serviceProvider;

        await InitLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_serviceProvider != null)
                return _serviceProvider;

            var connectionString = await ConnectionStringResolver
                .ResolveAsync(cancellationToken)
                .ConfigureAwait(false);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection, connectionString);
            _serviceProvider = serviceCollection.BuildServiceProvider();
            return _serviceProvider;
        }
        finally
        {
            InitLock.Release();
        }
    }
}
