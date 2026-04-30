using ScrumPokerAPI.Entities;
using ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;
using ScrumPokerAPI.Models;

namespace ScrumPokerAPI.Factories.RoomStateViewModelFactory;

public sealed class RoomStateViewModelFactory : IRoomStateViewModelFactory
{
    public RoomStateDTO FromRoom(Room room)
    {
        return new RoomStateDTO
        {
            RoomCode = room.Code,
            IsRevealed = room.IsRevealed,
            Participants = [
				.. room.Participants.Select(participant => new ParticipantStateDTO
				{
					DisplayName = participant.DisplayName,
					HasVoted = !string.IsNullOrEmpty(participant.Vote),
					Vote = room.IsRevealed ? participant.Vote : null,
				})
			],
        };
    }
}
