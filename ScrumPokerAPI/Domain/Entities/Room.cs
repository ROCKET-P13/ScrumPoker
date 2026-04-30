namespace ScrumPokerAPI.Domain.Entities;

public class Room
{
    public Guid Id { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public bool IsRevealed { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

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
            IsRevealed = false,
            CreatedAt = DateTimeOffset.UtcNow,
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
        IsRevealed = true;
    }

    public void ResetRound()
    {
        IsRevealed = false;
        foreach (var participant in Participants)
            participant.ClearVote();
    }
}
