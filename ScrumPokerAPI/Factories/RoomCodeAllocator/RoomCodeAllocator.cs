using ScrumPokerAPI.Factories.RoomCodeAllocator.Interfaces;
using ScrumPokerAPI.Repositories.RoomRepository.Interfaces;

namespace ScrumPokerAPI.Factories.RoomCodeAllocator;

public sealed class RoomCodeAllocator(IRoomRepository roomRepository) : IRoomCodeAllocator
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int MaxAttempts = 50;
    private const int CodeLength = 6;

    private readonly IRoomRepository _roomRepository = roomRepository;

    public async Task<string> AllocateAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = new string(Enumerable.Range(0, CodeLength)
                .Select(_ => CodeAlphabet[Random.Shared.Next(CodeAlphabet.Length)])
                .ToArray());
            var allocated = await _roomRepository.IsRoomCodeAllocatedAsync(code, cancellationToken).ConfigureAwait(false);
            if (!allocated)
                return code;
        }

        throw new InvalidOperationException("Could not allocate a unique room code.");
    }
}
