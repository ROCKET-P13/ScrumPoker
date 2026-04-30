using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Factories.RoomFactory.Interfaces;

public interface IRoomFactory
{
    Task<Room> FromDtos(CreateRoomRequestDto dto, string connectionId, CancellationToken cancellationToken);
}
