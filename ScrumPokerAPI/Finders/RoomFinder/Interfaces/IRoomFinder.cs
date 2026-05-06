using ScrumPokerAPI.Entities;

namespace ScrumPokerAPI.Finders.RoomFinder.Interfaces;

public interface IRoomFinder
{
    Task<Room?> FindById(Guid roomId, CancellationToken cancellationToken);

    Task<Room?> FindByCode(string roomCode, CancellationToken cancellationToken);

    Task<bool> AnyWithCodeAsync(string code, CancellationToken cancellationToken);
}
