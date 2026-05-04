using ScrumPokerAPI.Factories.ParticipantFactory.DTOs;
using ScrumPokerAPI.Factories.ParticipantFactory.Interfaces;
using ScrumPokerAPI.Factories.RoomFactory.Interfaces;
using ScrumPokerAPI.Factories.RoomStateViewModelFactory.Interfaces;
using ScrumPokerAPI.Finders.ParticipantFinder.Interfaces;
using ScrumPokerAPI.Finders.RoomFinder.Interfaces;
using ScrumPokerAPI.Models;
using ScrumPokerAPI.Models.Requests;
using ScrumPokerAPI.Persistence.Interfaces;
using ScrumPokerAPI.Repositories.RoomRepository.Interfaces;
using ScrumPokerAPI.Services.RoomService.Interfaces;

namespace ScrumPokerAPI.Services.RoomService;

public sealed class RoomService(
    IRoomRepository roomRepository,
    IUnitOfWork unitOfWork,
    IRoomFinder roomFinder,
    IParticipantFinder participantFinder,
    IRoomFactory roomFactory,
    IParticipantFactory participantFactory,
    IRoomStateViewModelFactory roomStateViewModelFactory
) : IRoomService
{
    private readonly IRoomRepository _roomRepository = roomRepository;
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
                IsRoomAdmin = true,
            });

        room.AddParticipant(participant);

        _roomRepository.Upsert(room);
        await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);

        return _roomStateViewModelFactory.FromRoom(room);
    }

    public async Task<RoomStateViewModel?> JoinRoom(string connectionId, JoinRoomRequestDTO dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var roomCode = dto.RoomCode.Trim().ToUpperInvariant();

        var room = await _roomRepository.FindByCode(roomCode, cancellationToken)
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
                return _roomStateViewModelFactory.FromRoom(room);
            }

            var oldRoom = await _roomRepository.FindById(existingConnection.RoomId, cancellationToken)
                .ConfigureAwait(false);
            var toRemove = oldRoom?.Participants.FirstOrDefault(p => p.ConnectionId == connectionId);
            if (toRemove != null && oldRoom != null)
            {
                oldRoom.RemoveParticipant(toRemove);
                await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);
            }
        }

        var newParticipant = _participantFactory.FromDto(
            new ParticipantFactoryDTO
            {
                ConnectionId = connectionId,
                DisplayName = dto.DisplayName,
                RoomId = room.Id,
            }
		);

        room.AddParticipant(newParticipant);
        _roomRepository.Upsert(room);

        await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);

        return _roomStateViewModelFactory.FromRoom(room);
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

        return _roomStateViewModelFactory.FromRoom(room);
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

        return _roomStateViewModelFactory.FromRoom(room);
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

        return _roomStateViewModelFactory.FromRoom(room);
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
        room.RemoveParticipant(participant);

        await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);
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

	public async Task CleanupRooms(CancellationToken cancellationToken)
	{
		// var now = DateTime.UtcNow;

		var staleRooms = await _roomRepository.FindStale(cancellationToken).ConfigureAwait(false);
		foreach( var room in staleRooms)
		{
			_roomRepository.Remove(room);
		}

		await _unitOfWork.SaveChanges(cancellationToken).ConfigureAwait(false);
		// for	
	}
}
