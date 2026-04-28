namespace ScrumPokerAPI.Core.Interfaces;

public interface IWebSocketClient
{
	Task SendMessageAsync(string connectionId, string message);
}