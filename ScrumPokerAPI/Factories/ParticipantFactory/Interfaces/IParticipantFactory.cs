using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Factories.ParticipantFactory.DTOs;

namespace ScrumPokerAPI.Factories.ParticipantFactory.Interfaces;

public interface IParticipantFactory
{
    Participant FromDto(ParticipantFactoryDTO dto);
}
