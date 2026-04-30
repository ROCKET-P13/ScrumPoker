using ScrumPokerAPI.Factories.RoomCodeAllocator.Interfaces;
using ScrumPokerAPI.Finders.RoomFinder.Interfaces;

namespace ScrumPokerAPI.Factories.RoomCodeAllocator;

public sealed class RoomCodeAllocator(IRoomFinder roomFinder) : IRoomCodeAllocator
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int MaxAttempts = 50;
    private const int CodeLength = 6;

    private readonly IRoomFinder _roomFinder = roomFinder;

    public async Task<string> Allocate(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var code = new string([.. Enumerable.Range(0, CodeLength).Select(_ => CodeAlphabet[Random.Shared.Next(CodeAlphabet.Length)])]);
            var allocated = await _roomFinder.AnyWithCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (!allocated)
			{
                return code;
			}
        }

        throw new InvalidOperationException("Could not allocate a unique room code.");
    }
}
