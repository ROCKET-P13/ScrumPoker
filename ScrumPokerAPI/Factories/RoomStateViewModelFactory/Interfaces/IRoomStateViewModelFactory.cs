using ScrumPokerAPI.Entities;
using ScrumPokerAPI.Models;

namespace ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;

public interface IRoomStateViewModelFactory
{
    RoomStateViewModel FromRoom(Room room);
}
