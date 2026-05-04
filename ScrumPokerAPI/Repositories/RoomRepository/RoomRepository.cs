using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Entities;
using ScrumPokerAPI.Repositories.RoomRepository.Interfaces;

namespace ScrumPokerAPI.Repositories.RoomRepository;

public sealed class RoomRepository(AppDatabaseContext databaseContext) : IRoomRepository
{
    private readonly AppDatabaseContext _databaseContext = databaseContext;

    public Task<Room?> FindById(Guid roomId, CancellationToken cancellationToken)
    {
        return _databaseContext.Rooms
            .Where(room => room.Id == roomId)
            .Include(room => room.Participants)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Room?> FindByCode(string roomCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roomCode);

        return _databaseContext.Rooms
            .Where(room => room.Code == roomCode)
            .Include(room => room.Participants)
            .FirstOrDefaultAsync(cancellationToken);
    }

	public Task<List<Room>> FindStale(CancellationToken cancellationToken)
	{
		return _databaseContext.Rooms
			.Where(room => room.EmptySince != null && (DateTime.UtcNow - room.EmptySince.Value).TotalSeconds > 60)
			.ToListAsync(cancellationToken);
	}

    public void Upsert(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        var roomEntry = _databaseContext.Entry(room);
        if (roomEntry.State == EntityState.Detached)
        {
            _databaseContext.Rooms.Add(room);
            return;
        }

        foreach (var participant in room.Participants)
        {
            if (_databaseContext.Entry(participant).State == EntityState.Modified)
                _databaseContext.Participants.Add(participant);
        }
    }

    public void Remove(Room room)
    {
        _databaseContext.Rooms.Remove(room);
    }
}
