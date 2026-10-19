using ChatInbox.Domain.Contacts;
using ChatInbox.Domain.Conversations;
using ChatInbox.Domain.Messages;

namespace ChatInbox.Application.Abstractions;

public interface IInboxRepository
{
    Task<Message?> GetMessageByTelegramUpdateIdAsync(
        long telegramUpdateId,
        CancellationToken cancellationToken);

    Task<Contact?> GetContactByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken);

    Task<Conversation?> GetConversationByPublicIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken);

    Task<Message> GetLatestMessageAsync(
        long conversationId,
        CancellationToken cancellationToken);

    void Add(Contact contact);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
