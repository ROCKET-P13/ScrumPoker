using System.Text.Json;
using ScrumPokerAPI.Core.Handlers;
using ScrumPokerAPI.Core.Messages;
using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Services;

public class HandlerRegistry(JoinRoomHandler joinRoomHandler, VoteHandler voteHandler)
{
	private readonly JoinRoomHandler _joinRoomHandler = joinRoomHandler;
	private readonly VoteHandler _voteHandler = voteHandler;

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
				await _joinRoomHandler.Handle(join!, request);
				break;

			case "SEND_VOTE":
				var vote = JsonSerializer.Deserialize<SendVoteMessage>(request.Body!, options);
				await _voteHandler.Send(vote!, request);
				break;

			case "REVEAL_VOTES":
				var revealVotesMessage = JsonSerializer.Deserialize<RevealVotesMessage>(request.Body!, options);
				await _voteHandler.Reveal(revealVotesMessage!);
				break;

			case "RESET_ROUND":
				var resetRoundMessage = JsonSerializer.Deserialize<ResetRoundMessage>(request.Body!, options);
				await _voteHandler.ResetRound(resetRoundMessage!);
				break;

			default:
				Console.WriteLine($"Unknown Type: {type}");
				break;
		}
	}
}