using ChatInbox.Contracts.IntegrationEvents;

namespace ChatInbox.Application.Messages;

public interface IProcessOutgoingMessageHandler
{
    Task<ConversationUpdatedIntegrationEvent?> SendAsync(
        OutgoingTextMessageIntegrationEvent outgoingMessage,
        CancellationToken cancellationToken);

    Task<ConversationUpdatedIntegrationEvent?> MarkFailedAsync(
        Guid messageId,
        CancellationToken cancellationToken);
}
