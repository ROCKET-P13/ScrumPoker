using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Persistence;
using ScrumPokerAPI.Persistence.Interfaces;
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
using ScrumPokerAPI.Services.ConnectionStringResolver;
using ScrumPokerAPI.Services.RoomService;
using ScrumPokerAPI.Services.RoomService.Interfaces;
using ScrumPokerAPI.Services.WebSocketRequestHandler;
using ScrumPokerAPI.Finders.ParticipantFinder.Interfaces;
using ScrumPokerAPI.Finders.ParticipantFinder;
using ScrumPokerAPI.Finders.RoomFinder.Interfaces;
using ScrumPokerAPI.Finders.RoomFinder;

namespace ScrumPokerAPI;

public static class LambdaStartup
{
    private static readonly SemaphoreSlim InitLock = new(1, 1);
    private static ServiceProvider? _serviceProvider;

    public static void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<AppDatabaseContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IRoomFinder, RoomFinder>();
        services.AddScoped<IParticipantFinder, ParticipantFinder>();
        services.AddScoped<IRoomCodeAllocator, RoomCodeAllocator>();
        services.AddScoped<IRoomFactory, RoomFactory>();
        services.AddScoped<IParticipantFactory, ParticipantFactory>();
        services.AddSingleton<IRoomStateViewModelFactory, RoomStateViewModelFactory>();
        services.AddScoped<IRoomService, RoomService>();
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
