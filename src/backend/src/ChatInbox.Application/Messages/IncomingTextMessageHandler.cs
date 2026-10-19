using ChatInbox.Application.Abstractions;
using ChatInbox.Contracts.IntegrationEvents;
using ChatInbox.Contracts.Realtime;
using ChatInbox.Domain.Contacts;
using ChatInbox.Domain.Messages;

namespace ChatInbox.Application.Messages;

public sealed class IncomingTextMessageHandler(IInboxRepository repository)
    : IIncomingTextMessageHandler
{
    public async Task<ConversationUpdatedIntegrationEvent> HandleAsync(
        IncomingTextMessageIntegrationEvent incomingMessage,
        CancellationToken cancellationToken)
    {
        var existingMessage = await repository.GetMessageByTelegramUpdateIdAsync(
            incomingMessage.UpdateId,
            cancellationToken);

        if (existingMessage is not null)
        {
            var latestMessage = await repository.GetLatestMessageAsync(
                existingMessage.ConversationId,
                cancellationToken);
            return CreateIntegrationEvent(latestMessage);
        }

        var contact = await repository.GetContactByTelegramUserIdAsync(
            incomingMessage.UserId,
            cancellationToken);

        if (contact is null)
        {
            contact = Contact.Create(
                incomingMessage.UserId,
                incomingMessage.FirstName,
                incomingMessage.LastName,
                incomingMessage.Username);
            repository.Add(contact);
        }
        else
        {
            contact.UpdateProfile(
                incomingMessage.FirstName,
                incomingMessage.LastName,
                incomingMessage.Username);
        }

        var conversation = contact.StartConversation(incomingMessage.ChatId);
        var message = conversation.ReceiveMessage(
            incomingMessage.UpdateId,
            incomingMessage.MessageId,
            incomingMessage.Text,
            incomingMessage.TelegramSentAt,
            incomingMessage.ReceivedAt);

        await repository.SaveChangesAsync(cancellationToken);

        var latestMessageAfterSave = await repository.GetLatestMessageAsync(
            message.ConversationId,
            cancellationToken);
        return CreateIntegrationEvent(latestMessageAfterSave);
    }

    private static ConversationUpdatedIntegrationEvent CreateIntegrationEvent(Message message)
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
                new MessageSummary(
                    message.PublicId,
                    message.Text,
                    "incoming",
                    message.TelegramSentAt,
                    message.ReceivedAt),
                conversation.UnreadCount,
                conversation.LastMessageAt ?? message.TelegramSentAt));
    }
}
