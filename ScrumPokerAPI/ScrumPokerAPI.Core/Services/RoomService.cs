using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Services;

public class RoomService
{
	private readonly Dictionary<string, Room> _rooms = [];
	private readonly Dictionary<string, string> _connectionToRoom = [];

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

		_connectionToRoom[player.ConnectionId] = roomId;
	}

	public Room? RemovePlayer(string connectionId)
	{
		if (!_connectionToRoom.TryGetValue(connectionId, out var roomId))
		{
			return null;
		}

		if (!_rooms.TryGetValue(roomId, out var room))
		{
			return null;
		}

		room.Players.RemoveAll(p => p.ConnectionId == connectionId);

		if (room.Players.Count == 0)
		{
			_rooms.Remove(roomId);
			_connectionToRoom.Remove(connectionId);
			return null;
		}

		_connectionToRoom.Remove(connectionId);
		return room;
	}

	public Room GetRoom(string roomId) => _rooms[roomId];
}