using ScrumPokerAPI.Entities;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Factories.RoomFactory.Interfaces;

public interface IRoomFactory
{
    Task<Room> FromDtos(CreateRoomRequestDTO dto, string connectionId, CancellationToken cancellationToken);
}
