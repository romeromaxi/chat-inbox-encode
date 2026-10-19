using ChatInbox.Contracts.IntegrationEvents;

namespace ChatInbox.Application.Messages;

public interface IIncomingTextMessageHandler
{
    Task<ConversationUpdatedIntegrationEvent> HandleAsync(
        IncomingTextMessageIntegrationEvent incomingMessage,
        CancellationToken cancellationToken);
}
