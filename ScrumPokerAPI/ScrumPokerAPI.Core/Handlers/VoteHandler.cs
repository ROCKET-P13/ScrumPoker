using ScrumPokerAPI.Core.Messages;
using ScrumPokerAPI.Core.Models;
using ScrumPokerAPI.Core.Services;

namespace ScrumPokerAPI.Core.Handlers;

public class VoteHandler(RoomService roomService)
{
	private readonly RoomService _roomService = roomService;

	public void Send(SendVoteMessage message, SocketRequest socketRequest)
	{
		_roomService.SetVote(message.RoomId, socketRequest.ConnectionId, message.Vote);
	}
	
	public void Reveal(RevealVotesMessage message)
	{
		_roomService.RevealVotes(message.RoomId);
	}

	public void ResetRound(ResetRoundMessage message)
	{
		_roomService.ResetRoom(message.RoomId);
	}
}