using System.Text.Json;
using ScrumPokerAPI.Core.Handlers;
using ScrumPokerAPI.Core.Messages;
using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Services;

public class HandlerRegistry(JoinRoomHandler joinRoomHandler, VoteHandler voteHandler, GameHub gameHub)
{
	private readonly JoinRoomHandler _joinRoomHandler = joinRoomHandler;
	private readonly VoteHandler _voteHandler = voteHandler;
	private readonly GameHub _gameHub = gameHub;

	public async Task Dispatch(SocketRequest request)
	{
		using var doc = JsonDocument.Parse(request.Body!);
		var type = doc.RootElement.GetProperty("type").GetString();

		var options = new JsonSerializerOptions
		{
			PropertyNameCaseInsensitive = true
		};

		switch (type)
		{
			case "JOIN_ROOM":
				var join = JsonSerializer.Deserialize<JoinRoomMessage>(request.Body!, options);
				_joinRoomHandler.Handle(join!, request);
				await _gameHub.BroadcastRoom(join!.RoomId);
				break;

			case "SEND_VOTE":
				var vote = JsonSerializer.Deserialize<SendVoteMessage>(request.Body!, options);
				_voteHandler.Send(vote!, request);
				await _gameHub.BroadcastRoom(vote!.RoomId);
				break;

			case "REVEAL_VOTES":
				var revealVotesMessage = JsonSerializer.Deserialize<RevealVotesMessage>(request.Body!, options);
				_voteHandler.Reveal(revealVotesMessage!);
				await _gameHub.BroadcastRoom(revealVotesMessage!.RoomId);
				break;

			case "RESET_ROUND":
				var resetRoundMessage = JsonSerializer.Deserialize<ResetRoundMessage>(request.Body!, options);
				_voteHandler.ResetRound(resetRoundMessage!);
				await _gameHub.BroadcastRoom(resetRoundMessage!.RoomId);
				break;

			default:
				Console.WriteLine($"Unknown Type: {type}");
				break;
		}
	}
}