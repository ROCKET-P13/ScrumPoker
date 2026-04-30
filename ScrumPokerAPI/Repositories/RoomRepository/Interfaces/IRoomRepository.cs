using ScrumPokerAPI.Domain.Entities;

namespace ScrumPokerAPI.Repositories.RoomRepository.Interfaces;

public interface IRoomRepository
{
    Task<bool> IsRoomCodeAllocatedAsync(string code, CancellationToken cancellationToken);

    Task<Guid?> FindRoomIdByCodeAsync(string normalizedRoomCode, CancellationToken cancellationToken);

    Task<Room?> GetRoomByIdForMutationAsync(Guid roomId, CancellationToken cancellationToken);

    Task<bool> AnyParticipantInRoomAsync(Guid roomId, CancellationToken cancellationToken);

    Task<Participant?> FindParticipantTrackedAsync(string connectionId, CancellationToken cancellationToken);

    Task<Participant?> FindParticipantWithRoomForRevealAsync(string connectionId, CancellationToken cancellationToken);

    Task<Participant?> FindParticipantWithRoomAggregateAsync(string connectionId, CancellationToken cancellationToken);

    Task<Participant?> FindParticipantReadOnlyAsync(string connectionId, CancellationToken cancellationToken);

    Task<Room?> GetRoomReadOnlyAsync(Guid roomId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Participant>> ListParticipantsReadOnlyAsync(Guid roomId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetConnectionIdsForRoomAsync(Guid roomId, CancellationToken cancellationToken);

    void Add(Room room);

    void Add(Participant participant);

    void Remove(Room room);

    void Remove(Participant participant);

    Task Upsert(CancellationToken cancellationToken);
}
