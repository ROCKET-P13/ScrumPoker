using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Services;

public class RoomService
{
	private readonly object _sync = new();
	private readonly Dictionary<string, Room> _rooms = [];
	private readonly Dictionary<string, string> _connectionToRoom = [];

	public Room GetOrCreateRoom(string roomId)
	{
		lock (_sync)
		{
			return GetOrCreateRoomUnlocked(roomId);
		}
	}

	public bool TryGetRoom(string roomId, out Room? room)
	{
		lock (_sync)
		{
			return _rooms.TryGetValue(roomId, out room);
		}
	}

	public void AddPlayer(string roomId, Player player)
	{
		lock (_sync)
		{
			var room = GetOrCreateRoomUnlocked(roomId);

			room.Players.RemoveAll(p => p.ConnectionId == player.ConnectionId);
			room.Players.Add(player);

			_connectionToRoom[player.ConnectionId] = roomId;
		}
	}

	public Room? RemovePlayer(string connectionId)
	{
		lock (_sync)
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
	}

	public void SetVote(string roomId, string connectionId, string vote)
	{
		lock (_sync)
		{
			var room = GetOrCreateRoomUnlocked(roomId);
			room.Votes[connectionId] = vote;

			var player = room.Players.FirstOrDefault(p => p.ConnectionId == connectionId);

			if (player != null)
			{
				player.Vote = vote;
			}
		}
	}

	public void RevealVotes(string roomId)
	{
		lock (_sync)
		{
			var room = GetOrCreateRoomUnlocked(roomId);
			room.IsRevealed = true;
		}
	}

	public void ResetRoom(string roomId)
	{
		lock (_sync)
		{
			var room = GetOrCreateRoomUnlocked(roomId);

			room.IsRevealed = false;
			room.Votes.Clear();

			foreach (var p in room.Players)
			{
				p.Vote = null;
			}
		}
	}

	private Room GetOrCreateRoomUnlocked(string roomId)
	{
		if (!_rooms.TryGetValue(roomId, out var room))
		{
			room = new Room { Id = roomId };
			_rooms[roomId] = room;
		}

		return room;
	}
}
