using ScrumPokerAPI.Core.Models;

namespace ScrumPokerAPI.Core.Messages;

public interface IMessageHandler<T>
{
	Task Handle(T message, SocketRequest context);
}