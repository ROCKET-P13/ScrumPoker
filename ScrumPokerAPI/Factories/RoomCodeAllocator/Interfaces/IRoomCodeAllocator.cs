namespace ScrumPokerAPI.Factories.RoomCodeAllocator.Interfaces;

public interface IRoomCodeAllocator
{
    Task<string> Allocate(CancellationToken cancellationToken);
}
