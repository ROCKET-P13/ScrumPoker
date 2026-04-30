using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Finders.RoomFinder.Interfaces;

namespace ScrumPokerAPI.Finders.RoomFinder;

public sealed class RoomFinder(AppDatabaseContext databaseContext) : IRoomFinder
{
    private readonly AppDatabaseContext _databaseContext = databaseContext;

    public Task<Room?> FindByIdAsync(Guid roomId, bool includeParticipants, CancellationToken cancellationToken)
    {
        var query = _databaseContext.Rooms.AsNoTracking().Where(room => room.Id == roomId);
        query = includeParticipants ? query.Include(room => room.Participants) : query;
        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Room?> FindByCodeAsync(string normalizedRoomCode, bool includeParticipants, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(normalizedRoomCode);
        var query = _databaseContext.Rooms.AsNoTracking().Where(room => room.Code == normalizedRoomCode);
        query = includeParticipants ? query.Include(room => room.Participants) : query;
        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> AnyWithCodeAsync(string code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(code);
        return _databaseContext.Rooms.AsNoTracking()
            .AnyAsync(room => room.Code == code, cancellationToken);
    }
}
