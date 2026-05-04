using ScrumPokerAPI.Entities;

namespace ScrumPokerAPI.Repositories.RoomRepository.Interfaces;

public interface IRoomRepository
{
    Task<Room?> FindById(Guid roomId, CancellationToken cancellationToken);

    Task<Room?> FindByCode(string roomCode, CancellationToken cancellationToken);
	Task<List<Room>> FindStale(CancellationToken cancellationToken);

    void Upsert(Room room);

    void Remove(Room room);
}
