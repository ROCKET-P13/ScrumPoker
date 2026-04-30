namespace ScrumPokerAPI.Factories.ParticipantFactory.DTOs;

public class ParticipantFactoryDTO
{
	public required string ConnectionId { get; set; }
	public required string DisplayName { get; set; }
	public Guid RoomId { get; set; }

	public bool IsRoomAdmin { get; set; }
}