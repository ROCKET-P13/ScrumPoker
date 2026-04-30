using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Repositories.RoomRepository.Interfaces;

namespace ScrumPokerAPI.Repositories.RoomRepository;

public sealed class RoomRepository(AppDatabaseContext databaseContext) : IRoomRepository
{
    private readonly AppDatabaseContext _databaseContext = databaseContext;

    public IQueryable<Participant> Participants => _databaseContext.Participants;

    public Task<Room?> FindRoomByIdTrackedAsync(Guid roomId, bool includeParticipants, CancellationToken cancellationToken)
    {
        var query = _databaseContext.Rooms.Where(room => room.Id == roomId);
        return includeParticipants
            ? query.Include(room => room.Participants).FirstOrDefaultAsync(cancellationToken)
            : query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Room?> FindRoomByCodeTrackedAsync(string normalizedRoomCode, bool includeParticipants, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizedRoomCode);
        var query = _databaseContext.Rooms.Where(room => room.Code == normalizedRoomCode);
        return includeParticipants
            ? query.Include(room => room.Participants).FirstOrDefaultAsync(cancellationToken)
            : query.FirstOrDefaultAsync(cancellationToken);
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

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _databaseContext.SaveChangesAsync(cancellationToken);
    }
}
