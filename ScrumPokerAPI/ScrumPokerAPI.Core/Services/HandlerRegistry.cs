using System.Text.Json;
using ScrumPokerAPI.Core.Handlers;
using ScrumPokerAPI.Core.Messages;
using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Services;

public class HandlerRegistry(JoinRoomHandler joinRoomHandler, VoteHandler voteHandler, GameHub gameHub)
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true
	};

	private readonly JoinRoomHandler _joinRoomHandler = joinRoomHandler;
	private readonly VoteHandler _voteHandler = voteHandler;
	private readonly GameHub _gameHub = gameHub;

	public async Task Dispatch(SocketRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Body))
		{
			Console.WriteLine("Dispatch skipped: request body is empty.");
			return;
		}

		string? type;
		try
		{
			using var doc = JsonDocument.Parse(request.Body);
			if (!doc.RootElement.TryGetProperty("type", out var typeElement))
			{
				Console.WriteLine("Dispatch skipped: JSON missing \"type\" property.");
				return;
			}

			type = typeElement.GetString();
			if (string.IsNullOrEmpty(type))
			{
				Console.WriteLine("Dispatch skipped: \"type\" is empty.");
				return;
			}
		}
		catch (JsonException ex)
		{
			Console.WriteLine($"Dispatch skipped: invalid JSON. {ex.Message}");
			return;
		}

		try
		{
			switch (type)
			{
				case "JOIN_ROOM":
					var joinRoomMessage = JsonSerializer.Deserialize<JoinRoomMessage>(request.Body, JsonOptions);
					if (joinRoomMessage is null)
					{
						Console.WriteLine("Dispatch skipped: JOIN_ROOM deserialize returned null.");
						return;
					}

					_joinRoomHandler.Handle(joinRoomMessage, request);
					await _gameHub.BroadcastRoom(joinRoomMessage.RoomId);
					break;

				case "SEND_VOTE":
					var sendVoteMessage = JsonSerializer.Deserialize<SendVoteMessage>(request.Body, JsonOptions);
					if (sendVoteMessage is null)
					{
						Console.WriteLine("Dispatch skipped: SEND_VOTE deserialize returned null.");
						return;
					}

					_voteHandler.Send(sendVoteMessage, request);
					await _gameHub.BroadcastRoom(sendVoteMessage.RoomId);
					break;

				case "REVEAL_VOTES":
					var revealVotesMessage = JsonSerializer.Deserialize<RevealVotesMessage>(request.Body, JsonOptions);
					if (revealVotesMessage is null)
					{
						Console.WriteLine("Dispatch skipped: REVEAL_VOTES deserialize returned null.");
						return;
					}

					_voteHandler.Reveal(revealVotesMessage);
					await _gameHub.BroadcastRoom(revealVotesMessage.RoomId);
					break;

				case "RESET_ROUND":
					var resetRoundMessage = JsonSerializer.Deserialize<ResetRoundMessage>(request.Body, JsonOptions);
					if (resetRoundMessage is null)
					{
						Console.WriteLine("Dispatch skipped: RESET_ROUND deserialize returned null.");
						return;
					}

					_voteHandler.ResetRound(resetRoundMessage);
					await _gameHub.BroadcastRoom(resetRoundMessage.RoomId);
					break;

				default:
					Console.WriteLine($"Unknown message type: {type}");
					break;
			}
		}
		catch (JsonException ex)
		{
			Console.WriteLine($"Dispatch failed: JSON deserialization error. {ex.Message}");
		}
	}
}
