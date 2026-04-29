using ScrumPokerAPI.Models;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Services.RoomService.Interfaces;

public interface IRoomService
{
    Task<RoomStateDto> CreateRoomAsync(string connectionId, CreateRoomRequestDto dto, CancellationToken cancellationToken);

    Task<RoomStateDto?> JoinRoomAsync(string connectionId, JoinRoomRequestDto dto, CancellationToken cancellationToken);

    Task<RoomStateDto?> VoteAsync(string connectionId, VoteRequestDto dto, CancellationToken cancellationToken);

    Task<RoomStateDto?> RevealAsync(string connectionId, CancellationToken cancellationToken);

    Task<RoomStateDto?> ResetRoundAsync(string connectionId, CancellationToken cancellationToken);

    Task<Guid?> RemoveConnectionAsync(string connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetConnectionIdsForRoomAsync(Guid roomId, CancellationToken cancellationToken);

    Task<RoomStateDto?> GetStateForConnectionAsync(string connectionId, CancellationToken cancellationToken);

    Task<Guid?> GetRoomIdForConnectionAsync(string connectionId, CancellationToken cancellationToken);

    Task<RoomStateDto?> GetRoomStateAsync(Guid roomId, CancellationToken cancellationToken);
}
