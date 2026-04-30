using ScrumPokerAPI.Domain.Entities;

namespace ScrumPokerAPI.Repositories.RoomRepository.Interfaces;

public interface IRoomRepository
{
    IQueryable<Participant> Participants { get; }

    Task<Room?> FindRoomByIdTrackedAsync(Guid roomId, bool includeParticipants, CancellationToken cancellationToken);

    Task<Room?> FindRoomByCodeTrackedAsync(string normalizedRoomCode, bool includeParticipants, CancellationToken cancellationToken);

    void Add(Room room);

    void Add(Participant participant);

    void Remove(Room room);

    void Remove(Participant participant);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
