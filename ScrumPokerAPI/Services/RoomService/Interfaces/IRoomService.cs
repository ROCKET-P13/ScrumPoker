using ScrumPokerAPI.Models;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Services.RoomService.Interfaces;

public interface IRoomService
{
    Task<RoomStateDTO> CreateRoomAsync(string connectionId, CreateRoomRequestDto dto, CancellationToken cancellationToken);

    Task<RoomStateDTO?> JoinRoom(string connectionId, JoinRoomRequestDto dto, CancellationToken cancellationToken);

    Task<RoomStateDTO?> CaptureVote(string connectionId, VoteRequestDto dto, CancellationToken cancellationToken);

    Task<RoomStateDTO?> RevealVotes(string connectionId, CancellationToken cancellationToken);

    Task<RoomStateDTO?> ResetRound(string connectionId, CancellationToken cancellationToken);

    Task<Guid?> RemoveConnection(string connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetConnectionIdsForRoom(Guid roomId, CancellationToken cancellationToken);

    Task<Guid?> GetRoomIdForConnection(string connectionId, CancellationToken cancellationToken);

    Task<RoomStateDTO?> GetRoomState(Guid roomId, CancellationToken cancellationToken);
}
