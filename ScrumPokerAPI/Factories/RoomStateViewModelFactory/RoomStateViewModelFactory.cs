using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;
using ScrumPokerAPI.Models;

namespace ScrumPokerAPI.Factories.RoomStateViewModelFactory;

public sealed class RoomStateViewModelFactory : IRoomStateViewModelFactory
{
    public RoomStateDto FromEntities(Room room, IReadOnlyList<Participant> participants)
    {
        return new RoomStateDto
        {
            RoomCode = room.Code,
            Revealed = room.Revealed,
            Participants = participants.Select(participant => new ParticipantStateDto
            {
                DisplayName = participant.DisplayName,
                HasVoted = !string.IsNullOrEmpty(participant.VoteValue),
                Vote = room.Revealed ? participant.VoteValue : null,
            }).ToList(),
        };
    }
}
