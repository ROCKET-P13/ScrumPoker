using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ScrumPokerAPI.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDatabaseContext>
{
    public AppDatabaseContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=127.0.0.1;Port=5432;Database=scrumpoker;Username=postgres;Password=postgres";
        var options = new DbContextOptionsBuilder<AppDatabaseContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AppDatabaseContext(options);
    }
}
