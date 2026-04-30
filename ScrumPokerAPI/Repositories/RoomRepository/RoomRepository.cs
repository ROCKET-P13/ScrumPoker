using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Repositories.RoomRepository.Interfaces;

namespace ScrumPokerAPI.Repositories.RoomRepository;

public sealed class RoomRepository(AppDatabaseContext databaseContext) : IRoomRepository
{
    private readonly AppDatabaseContext _databaseContext = databaseContext;

    public Task<bool> IsRoomCodeAllocatedAsync(string code, CancellationToken cancellationToken)
    {
        return _databaseContext.Rooms.AnyAsync(room => room.Code == code, cancellationToken);
    }

    public async Task<Guid?> FindRoomIdByCodeAsync(string normalizedRoomCode, CancellationToken cancellationToken)
    {
        return await _databaseContext.Rooms.AsNoTracking()
            .Where(room => room.Code == normalizedRoomCode)
            .Select(room => (Guid?)room.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task<Room?> GetRoomByIdForMutationAsync(Guid roomId, CancellationToken cancellationToken)
    {
        return _databaseContext.Rooms
            .FirstOrDefaultAsync(room => room.Id == roomId, cancellationToken);
    }

    public Task<bool> AnyParticipantInRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        return _databaseContext.Participants
            .AnyAsync(participant => participant.RoomId == roomId, cancellationToken);
    }

    public Task<Participant?> FindParticipantTrackedAsync(string connectionId, CancellationToken cancellationToken)
    {
        return _databaseContext.Participants
            .FirstOrDefaultAsync(participant => participant.ConnectionId == connectionId, cancellationToken);
    }

    public Task<Participant?> FindParticipantWithRoomForRevealAsync(string connectionId, CancellationToken cancellationToken)
    {
        return _databaseContext.Participants
            .Include(participant => participant.Room)
            .FirstOrDefaultAsync(participant => participant.ConnectionId == connectionId, cancellationToken);
    }

    public Task<Participant?> FindParticipantWithRoomAggregateAsync(string connectionId, CancellationToken cancellationToken)
    {
        return _databaseContext.Participants
            .Include(participant => participant.Room)
                .ThenInclude(room => room.Participants)
            .FirstOrDefaultAsync(participant => participant.ConnectionId == connectionId, cancellationToken);
    }

    public Task<Participant?> FindParticipantReadOnlyAsync(string connectionId, CancellationToken cancellationToken)
    {
        return _databaseContext.Participants.AsNoTracking()
            .FirstOrDefaultAsync(participant => participant.ConnectionId == connectionId, cancellationToken);
    }

    public Task<Room?> GetRoomReadOnlyAsync(Guid roomId, CancellationToken cancellationToken)
    {
        return _databaseContext.Rooms.AsNoTracking()
            .FirstOrDefaultAsync(room => room.Id == roomId, cancellationToken);
    }

    public async Task<IReadOnlyList<Participant>> ListParticipantsReadOnlyAsync(Guid roomId, CancellationToken cancellationToken)
    {
        return await _databaseContext.Participants.AsNoTracking()
            .Where(participant => participant.RoomId == roomId)
            .OrderBy(participant => participant.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<string>> GetConnectionIdsForRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        return await _databaseContext.Participants.AsNoTracking()
            .Where(participant => participant.RoomId == roomId)
            .Select(participant => participant.ConnectionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Add(Room room)
    {
        _databaseContext.Rooms.Add(room);
    }

    public void Add(Participant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        _databaseContext.Participants.Add(participant);
    }

    public void Remove(Room room)
    {
        _databaseContext.Rooms.Remove(room);
    }

    public void Remove(Participant participant)
    {
        _databaseContext.Participants.Remove(participant);
    }

    public Task Upsert(CancellationToken cancellationToken)
    {
        return _databaseContext.SaveChangesAsync(cancellationToken);
    }
}
