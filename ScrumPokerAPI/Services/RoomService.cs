using ScrumPokerAPI.Domain.Factories;
using ScrumPokerAPI.Domain.Repositories;
using ScrumPokerAPI.Models;
using ScrumPokerAPI.Models.Requests;
using ScrumPokerAPI.ViewModels.Factories;

namespace ScrumPokerAPI.Services;

public class RoomService(
    IRoomRepository roomRepository,
    IRoomFactory roomFactory,
    IParticipantFactory participantFactory,
    IRoomStateViewModelFactory roomStateViewModelFactory)
{
    private readonly IRoomRepository _roomRepository = roomRepository;
    private readonly IRoomFactory _roomFactory = roomFactory;
    private readonly IParticipantFactory _participantFactory = participantFactory;
    private readonly IRoomStateViewModelFactory _roomStateViewModelFactory = roomStateViewModelFactory;

    public async Task<RoomStateDto> CreateRoomAsync(string connectionId, CreateRoomRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var room = await _roomFactory.CreateFromDtoAsync(dto, connectionId, cancellationToken).ConfigureAwait(false);
        _roomRepository.Add(room);
        await _roomRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(room.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDto?> JoinRoomAsync(string connectionId, JoinRoomRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var normalized = dto.RoomCode.Trim().ToUpperInvariant();

        var room = await _roomRepository.GetRoomByCodeForMutationAsync(normalized, cancellationToken).ConfigureAwait(false);
        if (room == null)
            return null;

        var existingSameConnection = await _roomRepository.FindParticipantTrackedAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (existingSameConnection != null)
        {
            _roomRepository.Remove(existingSameConnection);
        }

        _participantFactory.AddFromJoinDto(dto, room, connectionId);
        await _roomRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(room.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDto?> VoteAsync(string connectionId, VoteRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var participant = await _roomRepository.FindParticipantTrackedAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (participant == null)
            return null;

        _participantFactory.ApplyVoteFromDto(participant, dto);
        await _roomRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(participant.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDto?> RevealAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _roomRepository.FindParticipantWithRoomForRevealAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (participant == null)
            return null;

        participant.Room.RevealVotes();
        await _roomRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(participant.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDto?> ResetRoundAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _roomRepository.FindParticipantWithRoomAggregateAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (participant == null)
            return null;

        var roomId = participant.RoomId;
        participant.Room.ResetRound();
        await _roomRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(roomId, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Removes participant by connection id; returns room id if they were in a room.</summary>
    public async Task<Guid?> RemoveConnectionAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _roomRepository.FindParticipantTrackedAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (participant == null)
            return null;

        var roomId = participant.RoomId;
        _roomRepository.Remove(participant);
        await _roomRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var anyLeft = await _roomRepository.AnyParticipantInRoomAsync(roomId, cancellationToken).ConfigureAwait(false);
        if (!anyLeft)
        {
            var room = await _roomRepository.GetRoomByIdForMutationAsync(roomId, cancellationToken).ConfigureAwait(false);
            if (room != null)
                _roomRepository.Remove(room);
            await _roomRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return roomId;
    }

    public Task<IReadOnlyList<string>> GetConnectionIdsForRoomAsync(Guid roomId, CancellationToken cancellationToken)
    {
        return _roomRepository.GetConnectionIdsForRoomAsync(roomId, cancellationToken);
    }

    public async Task<RoomStateDto?> GetStateForConnectionAsync(string connectionId, CancellationToken cancellationToken)
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

    public async Task<RoomStateDto?> GetRoomStateAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetRoomReadOnlyAsync(roomId, cancellationToken).ConfigureAwait(false);
        if (room == null)
            return null;

        var participants = await _roomRepository.ListParticipantsReadOnlyAsync(roomId, cancellationToken).ConfigureAwait(false);
        return _roomStateViewModelFactory.FromEntities(room, participants);
    }

    private async Task<RoomStateDto> ToRoomStateAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.GetRoomReadOnlyAsync(roomId, cancellationToken).ConfigureAwait(false);
        if (room == null)
            throw new InvalidOperationException("Room not found.");

        var participants = await _roomRepository.ListParticipantsReadOnlyAsync(roomId, cancellationToken).ConfigureAwait(false);
        return _roomStateViewModelFactory.FromEntities(room, participants);
    }
}
