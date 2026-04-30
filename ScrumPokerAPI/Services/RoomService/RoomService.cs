using Microsoft.EntityFrameworkCore;
using ScrumPokerAPI.Entities;
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
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(room.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDTO?> JoinRoom(string connectionId, JoinRoomRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var normalized = dto.RoomCode.Trim().ToUpperInvariant();

        var roomForLookup = await _roomFinder.FindByCode(normalized, cancellationToken)
            .ConfigureAwait(false);
        if (roomForLookup == null)
            return null;

        var existingSameConnection = await _participantRepository.Participants
            .FirstOrDefaultAsync(participant => participant.ConnectionId == connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (existingSameConnection != null)
        {
            if (existingSameConnection.RoomId == roomForLookup.Id)
            {
                existingSameConnection.UpdateDisplayName(dto.DisplayName);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return await ToRoomStateAsync(roomForLookup.Id, cancellationToken).ConfigureAwait(false);
            }

            _participantRepository.Remove(existingSameConnection);
        }

        var newParticipant = _participantFactory.FromDto(
            new ParticipantFactoryDTO
            {
                ConnectionId = connectionId,
                DisplayName = dto.DisplayName,
                RoomId = roomForLookup.Id,
            });
        _participantRepository.Add(newParticipant);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(roomForLookup.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDTO?> VoteAsync(string connectionId, VoteRequestDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var participant = await _participantRepository.Participants
            .FirstOrDefaultAsync(p => p.ConnectionId == connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (participant == null)
            return null;

        participant.RecordVote(dto.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(participant.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDTO?> RevealAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _participantRepository.Participants
            .Include(p => p.Room)
            .FirstOrDefaultAsync(p => p.ConnectionId == connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (participant == null)
            return null;

        participant.Room.RevealVotes();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(participant.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RoomStateDTO?> ResetRoundAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _participantRepository.Participants
            .Include(p => p.Room)
                .ThenInclude(room => room.Participants)
            .FirstOrDefaultAsync(p => p.ConnectionId == connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (participant == null)
            return null;

        var roomId = participant.RoomId;
        participant.Room.ResetRound();
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await ToRoomStateAsync(roomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid?> RemoveConnectionAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _participantRepository.Participants
            .FirstOrDefaultAsync(p => p.ConnectionId == connectionId, cancellationToken)
            .ConfigureAwait(false);
        if (participant == null)
            return null;

        var roomId = participant.RoomId;
        _participantRepository.Remove(participant);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await RemoveRoomIfEmptyAsync(roomId, cancellationToken).ConfigureAwait(false);

        return roomId;
    }

    public Task<IReadOnlyList<string>> GetConnectionIdsForRoom(Guid roomId, CancellationToken cancellationToken)
    {
        return _participantFinder.ListConnectionIdsForRoomAsync(roomId, cancellationToken);
    }

    public async Task<RoomStateDTO?> GetStateForConnectionAsync(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _participantFinder.FindByConnectionIdAsync(connectionId, cancellationToken).ConfigureAwait(false);
        if (participant == null)
            return null;

        return await ToRoomStateAsync(participant.RoomId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Guid?> GetRoomIdForConnection(string connectionId, CancellationToken cancellationToken)
    {
        var participant = await _participantFinder.FindByConnectionIdAsync(connectionId, cancellationToken).ConfigureAwait(false);
        return participant?.RoomId;
    }

    public async Task<RoomStateDTO?> GetRoomStateAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await _roomFinder.FindById(roomId, cancellationToken).ConfigureAwait(false);
        if (room == null)
            return null;

        var participants = OrderParticipantsByDisplayName(room);
        return _roomStateViewModelFactory.FromEntities(room, participants);
    }

    private async Task RemoveRoomIfEmptyAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await _roomRepository.FindById(roomId, cancellationToken)
            .ConfigureAwait(false);
        if (room == null)
            return;

        if (room.Participants.Count > 0)
            return;

        _roomRepository.Remove(room);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static IReadOnlyList<Participant> OrderParticipantsByDisplayName(Room room)
    {
        return [.. room.Participants.OrderBy(participant => participant.DisplayName)];
    }

    private async Task<RoomStateDTO> ToRoomStateAsync(Guid roomId, CancellationToken cancellationToken)
    {
        var room = await _roomFinder.FindById(roomId, cancellationToken).ConfigureAwait(false) ?? throw new InvalidOperationException("Room not found.");
        var participants = OrderParticipantsByDisplayName(room);
        return _roomStateViewModelFactory.FromEntities(room, participants);
    }
}
