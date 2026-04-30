using ScrumPokerAPI.Domain.Entities;
using ScrumPokerAPI.Factories.RoomCodeAllocator.Interfaces;
using ScrumPokerAPI.Factories.RoomFactory.Interfaces;
using ScrumPokerAPI.Models.Requests;

namespace ScrumPokerAPI.Factories.RoomFactory;

public sealed class RoomFactory(IRoomCodeAllocator roomCodeAllocator) : IRoomFactory
{
    private readonly IRoomCodeAllocator _roomCodeAllocator = roomCodeAllocator;

    public async Task<Room> FromDtos(CreateRoomRequestDto dto, string connectionId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var code = await _roomCodeAllocator.Allocate(cancellationToken).ConfigureAwait(false);
		
		var room = new Room
		{
			Id = Guid.NewGuid(),
            Code = code,
            CreatedAt = DateTimeOffset.UtcNow,
		};

        // room.AddParticipant(Guid.NewGuid(), connectionId, dto.DisplayName.Trim());
        return room;
    }
}
