using ChatInbox.Application.Abstractions;
using ChatInbox.Contracts.IntegrationEvents;

namespace ChatInbox.Application.Messages;

public sealed class QueueOutgoingMessageHandler(IInboxRepository repository)
    : IQueueOutgoingMessageHandler
{
    public async Task<QueueOutgoingMessageResult?> HandleAsync(
        Guid conversationId,
        string text,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var conversation = await repository.GetConversationByPublicIdAsync(
            conversationId,
            cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var message = conversation.SendMessage(text, createdAt);
        await repository.SaveChangesAsync(cancellationToken);

        var persistedMessage = await repository.GetMessageByPublicIdAsync(
            message.PublicId,
            cancellationToken) ?? throw new InvalidOperationException("The queued message could not be loaded");

        return new QueueOutgoingMessageResult(
            MessageContractMapper.ToSummary(persistedMessage),
            new OutgoingTextMessageIntegrationEvent(
                persistedMessage.PublicId,
                conversation.PublicId,
                conversation.TelegramChatId,
                persistedMessage.Text),
            ConversationUpdatedEventFactory.Create(persistedMessage));
    }
}
