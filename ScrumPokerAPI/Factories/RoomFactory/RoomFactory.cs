using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Factories.RoomCodeAllocator.Interfaces;
using ScrumPokerAPI.Factories.RoomFactory.Interfaces;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Factories.RoomFactory;

public sealed class RoomFactory(IRoomCodeAllocator roomCodeAllocator) : IRoomFactory
{
    private readonly IRoomCodeAllocator _roomCodeAllocator = roomCodeAllocator;

    public async Task<Room> CreateFromDtoAsync(CreateRoomRequestDto dto, string connectionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var code = await _roomCodeAllocator.AllocateAsync(cancellationToken).ConfigureAwait(false);
        var room = Room.CreateNew(code);
        room.AddParticipant(Guid.NewGuid(), connectionId, dto.DisplayName.Trim());
        return room;
    }
}
