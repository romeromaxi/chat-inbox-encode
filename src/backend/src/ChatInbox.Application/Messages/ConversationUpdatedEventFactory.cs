using ChatInbox.Contracts.IntegrationEvents;
using ChatInbox.Contracts.Realtime;
using ChatInbox.Domain.Messages;

namespace ChatInbox.Application.Messages;

public static class ConversationUpdatedEventFactory
{
    public static ConversationUpdatedIntegrationEvent Create(Message message)
    {
        var conversation = message.Conversation;
        var contact = conversation.Contact;

        return new ConversationUpdatedIntegrationEvent(
            new ConversationUpdatedEvent(
                conversation.PublicId,
                new ContactSummary(
                    contact.PublicId,
                    contact.FirstName,
                    contact.LastName,
                    contact.Username),
                MessageContractMapper.ToSummary(message),
                conversation.UnreadCount,
                conversation.LastMessageAt ?? message.TelegramSentAt));
    }
}
