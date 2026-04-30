namespace ScrumPokerAPI.Domain.Entities;

public class Participant
{
    public Guid Id { get; private set; }

    public Guid RoomId { get; private set; }

    public Room Room { get; private set; } = null!;

    public string ConnectionId { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string? Vote { get; private set; }

    // private Participant()
    // {
    // }

    internal static Participant CreateForRoom(Guid id, Room room, string connectionId, string displayName)
    {
        return new Participant
        {
            Id = id,
            RoomId = room.Id,
            Room = room,
            ConnectionId = connectionId,
            DisplayName = displayName,
            Vote = null,
        };
    }

    public void RecordVote(string value)
    {
        Vote = value.Trim();
    }

    public void ClearVote()
    {
        Vote = null;
    }
}
