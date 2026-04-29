using System.Text.Json;
using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Serialization;

public static class RoomStatePayload
{
	public static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNameCaseInsensitive = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	public static string Serialize(Room room) =>
		JsonSerializer.Serialize(new { type = "ROOM_STATE", room }, JsonOptions);
}
