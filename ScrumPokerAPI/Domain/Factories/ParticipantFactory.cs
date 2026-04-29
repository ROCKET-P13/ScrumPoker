using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Domain.Factories;

public sealed class ParticipantFactory : IParticipantFactory
{
    public Participant AddFromJoinDto(JoinRoomRequestDto dto, Room room, string connectionId)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ArgumentNullException.ThrowIfNull(room);

        return room.AddParticipant(Guid.NewGuid(), connectionId, dto.DisplayName.Trim());
    }

    public void ApplyVoteFromDto(Participant participant, VoteRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(participant);
        ArgumentNullException.ThrowIfNull(dto);

        participant.RecordVote(dto.Value);
    }
}
