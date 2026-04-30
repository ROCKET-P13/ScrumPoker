using ScrumPokerAPI.Factories.ParticipantFactory.DTOs;
using ScrumPokerAPI.Factories.ParticipantFactory.Interfaces;
using ScrumPokerAPI.Factories.RoomFactory.Interfaces;
using ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;
using ScrumPokerAPI.Models;
using ScrumPokerAPI.Models.Requests;
using ScrumPokerAPI.Repositories.RoomRepository.Interfaces;
using ScrumPokerAPI.Services.RoomService.Interfaces;

namespace ScrumPokerAPI.Services.RoomService;

public sealed class RoomService(
    IRoomRepository roomRepository,
    IRoomFactory roomFactory,
    IParticipantFactory participantFactory,
    IRoomStateViewModelFactory roomStateViewModelFactory
) : IRoomService
{
    private readonly IRoomRepository _roomRepository = roomRepository;
    private readonly IRoomFactory _roomFactory = roomFactory;
    private readonly IParticipantFactory _participantFactory = participantFactory;
    private readonly IRoomStateViewModelFactory _roomStateViewModelFactory = roomStateViewModelFactory;

    public async Task<RoomStateDTO> CreateRoomAsync(string connectionId, CreateRoomRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var room = await _roomFactory.FromDtos(dto, connectionId, cancellationToken).ConfigureAwait(false);
        var participant = _participantFactory.FromDto(
            new ParticipantFactoryDTO
            {
                ConnectionId = connectionId,
                DisplayName = dto.DisplayName,
                RoomId = room.Id,
            });

        room.AddParticipant(participant);

        _roomRepository.Add(room);
        await _roomRepository.Upsert(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(room.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDTO?> JoinRoomAsync(string connectionId, JoinRoomRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var normalized = dto.RoomCode.Trim().ToUpperInvariant();

        var roomId = await _roomRepository.FindRoomIdByCodeAsync(normalized, cancellationToken).ConfigureAwait(false);
        if (roomId == null)
            return null;

        var existingSameConnection = await _roomRepository.FindParticipantTrackedAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (existingSameConnection != null)
        {
            if (existingSameConnection.RoomId == roomId.Value)
            {
                existingSameConnection.UpdateDisplayName(dto.DisplayName);
                await _roomRepository.Upsert(cancellationToken).ConfigureAwait(false);
                return await ToRoomStateAsync(roomId.Value, cancellationToken).ConfigureAwait(false);
            }

            _roomRepository.Remove(existingSameConnection);
        }

        var newParticipant = _participantFactory.FromDto(
            new ParticipantFactoryDTO
            {
                ConnectionId = connectionId,
                DisplayName = dto.DisplayName,
                RoomId = roomId.Value,
            });
        _roomRepository.Add(newParticipant);

        await _roomRepository.Upsert(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(roomId.Value, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDTO?> VoteAsync(string connectionId, VoteRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var participant = await _roomRepository.FindParticipantTrackedAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (participant == null)
            return null;

        participant.RecordVote(dto.Value);
        await _roomRepository.Upsert(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(participant.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDTO?> RevealAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _roomRepository.FindParticipantWithRoomForRevealAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (participant == null)
            return null;

        participant.Room.RevealVotes();
        await _roomRepository.Upsert(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(participant.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDTO?> ResetRoundAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _roomRepository.FindParticipantWithRoomAggregateAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (participant == null)
            return null;

        var roomId = participant.RoomId;
        participant.Room.ResetRound();
        await _roomRepository.Upsert(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(roomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid?> RemoveConnectionAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _roomRepository.FindParticipantTrackedAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (participant == null)
            return null;

        var roomId = participant.RoomId;
        _roomRepository.Remove(participant);
        await _roomRepository.Upsert(cancellationToken).ConfigureAwait(false);

        var anyLeft = await _roomRepository.AnyParticipantInRoomAsync(roomId, cancellationToken).ConfigureAwait(false);
        if (!anyLeft)
        {
            var room = await _roomRepository.GetRoomByIdForMutationAsync(roomId, cancellationToken).ConfigureAwait(false);
            if (room != null)
                _roomRepository.Remove(room);
            await _roomRepository.Upsert(cancellationToken).ConfigureAwait(false);
        }

        return roomId;
    }

    public Task<IReadOnlyList<string>> GetConnectionIdsForRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        return _roomRepository.GetConnectionIdsForRoomAsync(roomId, cancellationToken);
    }

    public async Task<RoomStateDTO?> GetStateForConnectionAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _roomRepository.FindParticipantReadOnlyAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (participant == null)
            return null;

        return await ToRoomStateAsync(participant.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid?> GetRoomIdForConnectionAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _roomRepository.FindParticipantReadOnlyAsync(connectionId, cancellationToken).ConfigureAwait(false);
        return participant?.RoomId;
    }

    public async Task<RoomStateDTO?> GetRoomStateAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetRoomReadOnlyAsync(roomId, cancellationToken).ConfigureAwait(false);
        if (room == null)
            return null;

        var participants = await _roomRepository.ListParticipantsReadOnlyAsync(roomId, cancellationToken).ConfigureAwait(false);
        return _roomStateViewModelFactory.FromEntities(room, participants);
    }

    private async Task<RoomStateDTO> ToRoomStateAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetRoomReadOnlyAsync(roomId, cancellationToken).ConfigureAwait(false);
        if (room == null)
            throw new InvalidOperationException("Room not found.");

        var participants = await _roomRepository.ListParticipantsReadOnlyAsync(roomId, cancellationToken).ConfigureAwait(false);
        return _roomStateViewModelFactory.FromEntities(room, participants);
    }
}
