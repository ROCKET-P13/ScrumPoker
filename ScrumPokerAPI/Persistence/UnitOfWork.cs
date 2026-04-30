using ScrumPokerAPI.Data;
using ScrumPokerAPI.Persistence.Interfaces;

namespace ScrumPokerAPI.Persistence;

public sealed class UnitOfWork(AppDatabaseContext databaseContext) : IUnitOfWork
{
    private readonly AppDatabaseContext _databaseContext = databaseContext;

    public Task SaveChanges(CancellationToken cancellationToken)
    {
        return _databaseContext.SaveChangesAsync(cancellationToken);
    }
}
