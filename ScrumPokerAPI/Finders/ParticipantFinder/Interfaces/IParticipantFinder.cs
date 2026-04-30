using ScrumPokerAPI.Domain.Entities;

namespace ScrumPokerAPI.Finders.ParticipantFinder.Interfaces;

public interface IParticipantFinder
{
    Task<Participant?> FindByConnectionIdAsync(string connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListConnectionIdsForRoomAsync(Guid roomId, CancellationToken cancellationToken);
}
