namespace ScrumPokerAPI.Factories.RoomCodeAllocator.Interfaces;

public interface IRoomCodeAllocator
{
    Task<string> AllocateAsync(CancellationToken cancellationToken);
}
