namespace ScrumPokerAPI.Domain.Services;

/// <summary>Reserves a short room code that is not already persisted.</summary>
public interface IRoomCodeAllocator
{
    Task<string> AllocateAsync(CancellationToken cancellationToken);
}
