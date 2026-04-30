using ScrumPokerAPI.Domain.Entities;

namespace ScrumPokerAPI.Finders.RoomFinder.Interfaces;

public interface IRoomFinder
{
    Task<Room?> FindByIdAsync(Guid roomId, bool includeParticipants, CancellationToken cancellationToken);

    Task<Room?> FindByCodeAsync(string normalizedRoomCode, bool includeParticipants, CancellationToken cancellationToken);

    Task<bool> AnyWithCodeAsync(string code, CancellationToken cancellationToken);
}
