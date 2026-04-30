using System.Linq;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Repositories.ParticipantRepository.Interfaces;

namespace ScrumPokerAPI.Repositories.ParticipantRepository;

public sealed class ParticipantRepository(AppDatabaseContext databaseContext) : IParticipantRepository
{
    private readonly AppDatabaseContext _databaseContext = databaseContext;

    public IQueryable<Participant> Participants => _databaseContext.Participants;

    public void Add(Participant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        _databaseContext.Participants.Add(participant);
    }

    public void Remove(Participant participant)
    {
        _databaseContext.Participants.Remove(participant);
    }
}
