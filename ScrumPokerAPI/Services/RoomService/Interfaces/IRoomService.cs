using ScrumPokerAPI.Models;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Services.RoomService.Interfaces;

public interface IRoomService
{
    Task<RoomStateDTO> CreateRoomAsync(string connectionId, CreateRoomRequestDto dto, CancellationToken cancellationToken);

    Task<RoomStateDTO?> JoinRoomAsync(string connectionId, JoinRoomRequestDto dto, CancellationToken cancellationToken);

    Task<RoomStateDTO?> VoteAsync(string connectionId, VoteRequestDto dto, CancellationToken cancellationToken);

    Task<RoomStateDTO?> RevealAsync(string connectionId, CancellationToken cancellationToken);

    Task<RoomStateDTO?> ResetRoundAsync(string connectionId, CancellationToken cancellationToken);

    Task<Guid?> RemoveConnectionAsync(string connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetConnectionIdsForRoomAsync(Guid roomId, CancellationToken cancellationToken);

    Task<RoomStateDTO?> GetStateForConnectionAsync(string connectionId, CancellationToken cancellationToken);

    Task<Guid?> GetRoomIdForConnectionAsync(string connectionId, CancellationToken cancellationToken);

    Task<RoomStateDTO?> GetRoomStateAsync(Guid roomId, CancellationToken cancellationToken);
}
