using ScrumPokerAPI.Entities;
using ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;
using ScrumPokerAPI.Models;

namespace ScrumPokerAPI.Factories.RoomStateViewModelFactory;

public sealed class RoomStateViewModelFactory : IRoomStateViewModelFactory
{
    public RoomStateViewModel FromRoom(Room room)
    {
        return new RoomStateViewModel
        {
            RoomCode = room.Code,
            IsRevealed = room.IsRevealed,
            Participants = [
				.. room.Participants.Select(participant => new ParticipantViewModel
				{
					DisplayName = participant.DisplayName,
					IsRoomAdmin = participant.IsRoomAdmin,
					HasVoted = !string.IsNullOrEmpty(participant.Vote),
					Vote = room.IsRevealed ? participant.Vote : null,
				})
			],
        };
    }
}
