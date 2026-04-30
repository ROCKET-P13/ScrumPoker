using ScrumPokerAPI.Domain.Entities;

namespace ScrumPokerAPI.Finders.RoomFinder.Interfaces;

public interface IRoomFinder
{
    Task<Room?> FindById(Guid roomId, CancellationToken cancellationToken);

    Task<Room?> FindByCode(string normalizedRoomCode, CancellationToken cancellationToken);

    Task<bool> AnyWithCodeAsync(string code, CancellationToken cancellationToken);
}
