using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Factories.ParticipantFactory.Interfaces;

public interface IParticipantFactory
{
    Participant AddFromJoinDto(JoinRoomRequestDto dto, Room room, string connectionId);

    void ApplyVoteFromDto(Participant participant, VoteRequestDto dto);
}
