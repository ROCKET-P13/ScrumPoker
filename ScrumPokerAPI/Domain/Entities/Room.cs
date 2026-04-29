namespace ScrumPokerAPI.Domain.Entities;

public class Room
{
    public Guid Id { get; private set; }

    /// <summary>Short code clients use to join (e.g. 6 alphanumeric characters).</summary>
    public string Code { get; private set; } = string.Empty;

    public bool Revealed { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public ICollection<Participant> Participants { get; private set; } = new List<Participant>();

    private Room()
    {
    }

    public static Room CreateNew(string code)
    {
        return new Room
        {
            Id = Guid.NewGuid(),
            Code = code,
            Revealed = false,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Participants = new List<Participant>(),
        };
    }

    public Participant AddParticipant(Guid participantId, string connectionId, string displayName)
    {
        var participant = Participant.CreateForRoom(participantId, this, connectionId, displayName);
        Participants.Add(participant);
        return participant;
    }

    public void RevealVotes()
    {
        Revealed = true;
    }

    public void ResetRound()
    {
        Revealed = false;
        foreach (var participant in Participants)
            participant.ClearVote();
    }
}
