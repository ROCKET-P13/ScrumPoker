using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Services;

public class RoomService
{
	private readonly Dictionary<string, Room> _rooms = [];

	public Room GetOrCreateRoom(string roomId)
	{
		if (!_rooms.TryGetValue(roomId, out var room))
		{
			room = new Room { Id = roomId };
			_rooms[roomId] = room;
		}

		return room;
	}

	public void AddPlayer(string roomId, Player player)
	{
		var room = GetOrCreateRoom(roomId);

		room.Players.RemoveAll(p => p.ConnectionId == player.ConnectionId);
		room.Players.Add(player);
	}

	public Room GetRoom(string roomId) => _rooms[roomId];
}