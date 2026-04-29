using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Domain.Factories;

public interface IParticipantFactory
{
    Participant AddFromJoinDto(JoinRoomRequestDto dto, Room room, string connectionId);

    void ApplyVoteFromDto(Participant participant, VoteRequestDto dto);
}
