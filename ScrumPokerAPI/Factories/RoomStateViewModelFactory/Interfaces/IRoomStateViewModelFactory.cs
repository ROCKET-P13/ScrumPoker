using ScrumPokerAPI.Entities;
using ScrumPokerAPI.Models;

namespace ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;

public interface IRoomStateViewModelFactory
{
    RoomStateDTO FromEntities(Room room, IReadOnlyList<Participant> participants);
}
