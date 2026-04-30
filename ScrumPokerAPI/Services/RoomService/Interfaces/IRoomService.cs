using ScrumPokerAPI.Models;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Services.RoomService.Interfaces;

public interface IRoomService
{
    Task<RoomStateViewModel> CreateRoomAsync(string connectionId, CreateRoomRequestDTO dto, CancellationToken cancellationToken);

    Task<RoomStateViewModel?> JoinRoom(string connectionId, JoinRoomRequestDTO dto, CancellationToken cancellationToken);

    Task<RoomStateViewModel?> CaptureVote(string connectionId, VoteRequestDTO dto, CancellationToken cancellationToken);

    Task<RoomStateViewModel?> RevealVotes(string connectionId, CancellationToken cancellationToken);

    Task<RoomStateViewModel?> ResetRound(string connectionId, CancellationToken cancellationToken);

    Task<Guid?> RemoveConnection(string connectionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetConnectionIdsForRoom(Guid roomId, CancellationToken cancellationToken);

    Task<Guid?> GetRoomIdForConnection(string connectionId, CancellationToken cancellationToken);

    Task<RoomStateViewModel?> GetRoomState(Guid roomId, CancellationToken cancellationToken);
}
