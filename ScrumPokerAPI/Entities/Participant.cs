namespace ScrumPokerAPI.Entities;

public class Participant
{
    public Guid Id { get; set; }

    public Guid RoomId { get; set; }

    public Room Room { get; set; } = null!;

    public string ConnectionId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsRoomAdmin { get; set; }

    public string? Vote { get; private set; }

    public void UpdateDisplayName(string displayName)
    {
        ArgumentNullException.ThrowIfNull(displayName);
        DisplayName = displayName.Trim();
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
