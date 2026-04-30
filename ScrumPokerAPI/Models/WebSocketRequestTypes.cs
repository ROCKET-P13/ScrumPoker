namespace ScrumPokerAPI.Models;

public static class WebSocketRequestTypes
{
    public const string CreateRoom = "CREATE_ROOM";

    public const string JoinRoom = "JOIN_ROOM";

    public const string SendVote = "SEND_VOTE";

    public const string RevealVotes = "REVEAL_VOTES";

    public const string ResetRound = "RESET_ROUND";
}
