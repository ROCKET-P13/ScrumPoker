using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Data;
using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Finders.ParticipantFinder.Interfaces;

namespace ScrumPokerAPI.Finders.ParticipantFinder;

public sealed class ParticipantFinder(AppDatabaseContext databaseContext) : IParticipantFinder
{
    private readonly AppDatabaseContext _databaseContext = databaseContext;

    public Task<Participant?> FindByConnectionIdAsync(string connectionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        return _databaseContext.Participants.AsNoTracking()
            .FirstOrDefaultAsync(participant => participant.ConnectionId == connectionId, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListConnectionIdsForRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        return await _databaseContext.Participants.AsNoTracking()
            .Where(participant => participant.RoomId == roomId)
            .Select(participant => participant.ConnectionId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
