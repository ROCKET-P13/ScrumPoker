using System.Text.Json;
using ScrumPokerAPI.Core.Interfaces;
using ScrumPokerAPI.Core.Messages;
using ScrumPokerAPI.Core.Models;
using ScrumPokerAPI.Core.Services;

namespace ScrumPokerAPI.Core.Handlers;

public class VoteHandler(IWebSocketClient webSocketClient, RoomService roomService)
{
	private readonly IWebSocketClient _webSocketClient = webSocketClient;
	private readonly RoomService _roomService = roomService;

	public async Task Send(SendVoteMessage message, SocketRequest socketRequest)
	{
		_roomService.SetVote(message.RoomId, socketRequest.ConnectionId, message.Vote);
		var room = _roomService.GetOrCreateRoom(message.RoomId);
		await BroadcastRoom(room);
	}
	
	public async Task Reveal(RevealVotesMessage message)
	{
		_roomService.RevealVotes(message.RoomId);
		var room = _roomService.GetOrCreateRoom(message.RoomId);
		await BroadcastRoom(room);
	}

	public async Task ResetRound(ResetRoundMessage message)
	{
		var room = _roomService.GetOrCreateRoom(message.RoomId);
		_roomService.ResetRoom(message.RoomId);
		await BroadcastRoom(room);
	}

	private async Task BroadcastRoom(Room room)
	{
		var payload = JsonSerializer.Serialize(new
		{
			type = "ROOM_STATE",
			room
		});

		foreach (var player in room.Players)
		{
			await _webSocketClient.SendMessageAsync(player.ConnectionId, payload);
		}
	}
}