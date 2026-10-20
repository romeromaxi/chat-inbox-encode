using ChatInbox.Application.Abstractions;
using ChatInbox.Contracts.IntegrationEvents;
using ChatInbox.Domain.Messages;

namespace ChatInbox.Application.Messages;

public sealed class ProcessOutgoingMessageHandler(
    IInboxRepository repository,
    ITelegramMessageSender telegramMessageSender)
    : IProcessOutgoingMessageHandler
{
    public async Task<ConversationUpdatedIntegrationEvent?> SendAsync(
        OutgoingTextMessageIntegrationEvent outgoingMessage,
        CancellationToken cancellationToken)
    {
        var message = await repository.GetMessageByPublicIdAsync(
            outgoingMessage.MessageId,
            cancellationToken);

        if (message is null)
        {
            return null;
        }

        if (message.Status != MessageStatus.Pending)
        {
            return ConversationUpdatedEventFactory.Create(message);
        }

        var sentMessage = await telegramMessageSender.SendTextMessageAsync(
            outgoingMessage.TelegramChatId,
            outgoingMessage.Text,
            cancellationToken);

        message.MarkSent(sentMessage.MessageId, sentMessage.SentAt);
        await repository.SaveChangesAsync(cancellationToken);
        return ConversationUpdatedEventFactory.Create(message);
    }

    public async Task<ConversationUpdatedIntegrationEvent?> MarkFailedAsync(
        Guid messageId,
        CancellationToken cancellationToken)
    {
        var message = await repository.GetMessageByPublicIdAsync(messageId, cancellationToken);

        if (message is null || message.Status != MessageStatus.Pending)
        {
            return null;
        }

        message.MarkFailed();
        await repository.SaveChangesAsync(cancellationToken);
        return ConversationUpdatedEventFactory.Create(message);
    }
}
