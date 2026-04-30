using System.Linq;
using ScrumPokerAPI.Entities;

namespace ScrumPokerAPI.Repositories.ParticipantRepository.Interfaces;

public interface IParticipantRepository
{
    IQueryable<Participant> Participants { get; }

    void Add(Participant participant);

    void Remove(Participant participant);
}
