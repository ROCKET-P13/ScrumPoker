namespace ScrumPokerAPI.Entities;

public class Room
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public bool IsRevealed { get; private set; } = false;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTime? EmptySince { get; private set; }

    public ICollection<Participant> Participants { get; private set; } = new List<Participant>();

    public Participant AddParticipant(Participant participant)
    {
        Participants.Add(participant);
		if (EmptySince != null)
		{
			EmptySince = null;
		}
		
        return participant;
    }

    public void RemoveParticipant(Participant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        if (participant.RoomId != Id)
            throw new InvalidOperationException("Participant does not belong to this room.");
        if (!Participants.Remove(participant))
            throw new InvalidOperationException("Participant is not a member of this room.");

		if (Participants.Count <= 0)
		{
			EmptySince = DateTime.UtcNow;
		}
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
