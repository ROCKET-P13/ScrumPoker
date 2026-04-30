using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Factories.ParticipantFactory.DTOs;
using ScrumPokerAPI.Factories.ParticipantFactory.Interfaces;

namespace ScrumPokerAPI.Factories.ParticipantFactory;

public sealed class ParticipantFactory : IParticipantFactory
{
    public Participant FromDto(ParticipantFactoryDTO dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new Participant
        {
            Id = Guid.NewGuid(),
            RoomId = dto.RoomId,
            ConnectionId = dto.ConnectionId,
            DisplayName = dto.DisplayName.Trim(),
        };
    }
}