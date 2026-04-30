using ScrumPokerAPI.Factories.ParticipantFactory.DTOs;
using ScrumPokerAPI.Factories.ParticipantFactory.Interfaces;
using ScrumPokerAPI.Factories.RoomFactory.Interfaces;
using ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;
using ScrumPokerAPI.Finders.ParticipantFinder.Interfaces;
using ScrumPokerAPI.Finders.RoomFinder.Interfaces;
using ScrumPokerAPI.Models;
using ScrumPokerAPI.Models.Requests;
using ScrumPokerAPI.Persistence.Interfaces;
using ScrumPokerAPI.Repositories.ParticipantRepository.Interfaces;
using ScrumPokerAPI.Repositories.RoomRepository.Interfaces;
using ScrumPokerAPI.Services.RoomService.Interfaces;

namespace ScrumPokerAPI.Services.RoomService;

public sealed class RoomService(
    IRoomRepository roomRepository,
    IParticipantRepository participantRepository,
    IUnitOfWork unitOfWork,
    IRoomFinder roomFinder,
    IParticipantFinder participantFinder,
    IRoomFactory roomFactory,
    IParticipantFactory participantFactory,
    IRoomStateViewModelFactory roomStateViewModelFactory
) : IRoomService
{
    private readonly IRoomRepository _roomRepository = roomRepository;
    private readonly IParticipantRepository _participantRepository = participantRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IRoomFinder _roomFinder = roomFinder;
    private readonly IParticipantFinder _participantFinder = participantFinder;
    private readonly IRoomFactory _roomFactory = roomFactory;
    private readonly IParticipantFactory _participantFactory = participantFactory;
    private readonly IRoomStateViewModelFactory _roomStateViewModelFactory = roomStateViewModelFactory;

    public async Task<RoomStateViewModel> CreateRoomAsync(string connectionId, CreateRoomRequestDTO dto, CancellationToken cancellationToken)
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

        _roomRepository.Upsert(room);
        await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(room.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateViewModel?> JoinRoom(string connectionId, JoinRoomRequestDTO dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var normalized = dto.RoomCode.Trim().ToUpperInvariant();

        var room = await _roomFinder.FindByCode(normalized, cancellationToken)
            .ConfigureAwait(false);
        if (room == null)
            return null;

        var existingConnection = await _participantFinder.FindByConnectionId(connectionId, cancellationToken)
            .ConfigureAwait(false);

        if (existingConnection != null)
        {
            if (existingConnection.RoomId == room.Id)
            {
                var participant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
                if (participant == null)
                    return null;

                participant.UpdateDisplayName(dto.DisplayName);
                await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);
                return await ToRoomStateAsync(room.Id, cancellationToken).ConfigureAwait(false);
            }

            var oldRoom = await _roomRepository.FindById(existingConnection.RoomId, cancellationToken)
                .ConfigureAwait(false);
            var toRemove = oldRoom?.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (toRemove != null)
                _participantRepository.Remove(toRemove);
        }

        var newParticipant = _participantFactory.FromDto(
            new ParticipantFactoryDTO
            {
                ConnectionId = connectionId,
                DisplayName = dto.DisplayName,
                RoomId = room.Id,
            });
        _participantRepository.Add(newParticipant);

        await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(room.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateViewModel?> CaptureVote(string connectionId, VoteRequestDTO dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var lookup = await _participantFinder.FindByConnectionId(connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (lookup == null)
            return null;

        var room = await _roomRepository.FindById(lookup.RoomId, cancellationToken).ConfigureAwait(false);
        if (room == null)
            return null;

        var participant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
        if (participant == null)
            return null;

        participant.RecordVote(dto.Value);
        await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(participant.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateViewModel?> RevealVotes(string connectionId, CancellationToken cancellationToken)
    {
        var lookup = await _participantFinder.FindByConnectionId(connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (lookup == null)
            return null;

        var room = await _roomRepository.FindById(lookup.RoomId, cancellationToken).ConfigureAwait(false);
        if (room == null)
            return null;

        room.RevealVotes();
        await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(lookup.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateViewModel?> ResetRound(string connectionId, CancellationToken cancellationToken)
    {
        var lookup = await _participantFinder.FindByConnectionId(connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (lookup == null)
            return null;

        var room = await _roomRepository.FindById(lookup.RoomId, cancellationToken).ConfigureAwait(false);
        if (room == null)
            return null;

        room.ResetRound();
        await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(lookup.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid?> RemoveConnection(string connectionId, CancellationToken cancellationToken)
    {
        var lookup = await _participantFinder.FindByConnectionId(connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (lookup == null)
            return null;

        var room = await _roomRepository.FindById(lookup.RoomId, cancellationToken).ConfigureAwait(false);
        if (room == null)
            return null;

        var participant = room.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
        if (participant == null)
            return null;

        var roomId = participant.RoomId;
        _participantRepository.Remove(participant);
        await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);

        await RemoveRoomIfEmpty(roomId, cancellationToken).ConfigureAwait(false);

        return roomId;
    }

    public Task<IReadOnlyList<string>> GetConnectionIdsForRoom(Guid roomId, CancellationToken cancellationToken)
    {
        return _participantFinder.ListConnectionIdsForRoomAsync(roomId, cancellationToken);
    }

    public async Task<Guid?> GetRoomIdForConnection(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _participantFinder.FindByConnectionId(connectionId, cancellationToken).ConfigureAwait(false);
        return participant?.RoomId;
    }

    public async Task<RoomStateViewModel?> GetRoomState(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await _roomFinder.FindById(roomId, cancellationToken).ConfigureAwait(false);
        if (room == null)
            return null;

        return _roomStateViewModelFactory.FromRoom(room);
    }

    private async Task RemoveRoomIfEmpty(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.FindById(roomId, cancellationToken)
            .ConfigureAwait(false);

        if (room == null)
            return;

        if (room.Participants.Count > 0)
            return;

        _roomRepository.Remove(room);
        await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);
    }

    private async Task<RoomStateViewModel> ToRoomStateAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await _roomFinder.FindById(roomId, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Room not found.");
        return _roomStateViewModelFactory.FromRoom(room);
    }
}
