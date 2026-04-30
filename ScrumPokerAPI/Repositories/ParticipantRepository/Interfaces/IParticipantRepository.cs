using ScrumPokerAPI.Entities;

namespace ScrumPokerAPI.Repositories.ParticipantRepository.Interfaces;

public interface IParticipantRepository
{
    void Add(Participant participant);

    void Remove(Participant participant);
}
