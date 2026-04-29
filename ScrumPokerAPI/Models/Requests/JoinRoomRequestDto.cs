namespace ScrumPokerAPI.Models.Requests;

public sealed class JoinRoomRequestDto
{
    public string RoomCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
}
