using ScrumPokerAPI.Domain.Entities;

namespace ScrumPokerAPI.Repositories.RoomRepository.Interfaces;

public interface IRoomRepository
{
    Task<Room?> FindById(Guid roomId, CancellationToken cancellationToken);

    void Add(Room room);

    void Remove(Room room);
}
