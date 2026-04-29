using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Models;

namespace ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;

/// <summary>Maps domain aggregates to API-facing room state for WebSocket clients.</summary>
public interface IRoomStateViewModelFactory
{
    RoomStateDto FromEntities(Room room, IReadOnlyList<Participant> participants);
}
