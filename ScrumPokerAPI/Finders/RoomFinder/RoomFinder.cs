using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Entities;
using ScrumPokerAPI.Finders.RoomFinder.Interfaces;

namespace ScrumPokerAPI.Finders.RoomFinder;

public sealed class RoomFinder(AppDatabaseContext databaseContext) : IRoomFinder
{
    private readonly AppDatabaseContext _databaseContext = databaseContext;

    public Task<Room?> FindById(Guid roomId, CancellationToken cancellationToken)
    {
        return _databaseContext.Rooms
            .AsNoTracking()
            .Where(room => room.Id == roomId)
            .Include(room => room.Participants.OrderBy(participant => participant.DisplayName))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Room?> FindByCode(string roomCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roomCode);

        return _databaseContext.Rooms
            .AsNoTracking()
            .Where(room => room.Code == roomCode)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> AnyWithCodeAsync(string code, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(code);
        return _databaseContext.Rooms
            .AsNoTracking()
            .AnyAsync(room => room.Code == code, cancellationToken);
    }
}
