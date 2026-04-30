using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Domain.Entities;
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

    public void Add(Room room)
    {
        _databaseContext.Rooms.Add(room);
    }

    public void Remove(Room room)
    {
        _databaseContext.Rooms.Remove(room);
    }
}
