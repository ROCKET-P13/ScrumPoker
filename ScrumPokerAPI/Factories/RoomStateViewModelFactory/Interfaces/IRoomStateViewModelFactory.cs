using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Models;

namespace ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;

public interface IRoomStateViewModelFactory
{
    RoomStateDto FromEntities(Room room, IReadOnlyList<Participant> participants);
}
