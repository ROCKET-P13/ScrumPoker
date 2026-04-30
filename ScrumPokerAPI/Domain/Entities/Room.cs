namespace ScrumPokerAPI.Domain.Entities;

public class Room
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public bool IsRevealed { get; private set; } = false;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Participant> Participants { get; private set; } = [];

    public Participant AddParticipant(Participant participant)
    {
        participant.RoomId = Id;
        participant.Room = this;
        Participants.Add(participant);
        return participant;
    }

    public void RemoveParticipant(Participant participant)
    {
        Participants.Remove(participant);
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
