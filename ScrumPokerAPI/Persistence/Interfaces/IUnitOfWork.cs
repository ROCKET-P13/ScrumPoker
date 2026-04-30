namespace ScrumPokerAPI.Persistence.Interfaces;

public interface IUnitOfWork
{
    Task SaveChanges(CancellationToken cancellationToken);
}
