using ScrumPokerAPI.Core.Messages;
using ScrumPokerAPI.Core.Models;
using ScrumPokerAPI.Core.Services;

namespace ScrumPokerAPI.Core.Handlers;

public class JoinRoomHandler(RoomService roomService)
{
	private readonly RoomService _roomService = roomService;

	public void Handle(JoinRoomMessage message, SocketRequest socketRequest)
	{
		var player = new Player
		{
			ConnectionId = socketRequest.ConnectionId,
			Name = message.Name
		};

		_roomService.AddPlayer(message.RoomId, player);
	}
}