namespace ScrumPokerAPI.Models.Requests;

public sealed class JoinRoomRequestDTO
{
    public string RoomCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
