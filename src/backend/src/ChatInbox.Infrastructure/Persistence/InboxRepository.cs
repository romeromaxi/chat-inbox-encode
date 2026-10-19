using ChatInbox.Application.Abstractions;
using ChatInbox.Domain.Contacts;
using ChatInbox.Domain.Conversations;
using ChatInbox.Domain.Messages;
using Microsoft.EntityFrameworkCore;

namespace ChatInbox.Infrastructure.Persistence;

public sealed class InboxRepository(ChatInboxDbContext dbContext) : IInboxRepository
{
    public Task<Message?> GetMessageByTelegramUpdateIdAsync(
        long telegramUpdateId,
        CancellationToken cancellationToken) =>
        dbContext.Messages
            .Include(message => message.Conversation)
            .ThenInclude(conversation => conversation.Contact)
            .SingleOrDefaultAsync(
                message => message.TelegramUpdateId == telegramUpdateId,
                cancellationToken);

    public Task<Contact?> GetContactByTelegramUserIdAsync(
        long telegramUserId,
        CancellationToken cancellationToken) =>
        dbContext.Contacts
            .Include(contact => contact.Conversation)
            .SingleOrDefaultAsync(
                contact => contact.TelegramUserId == telegramUserId,
                cancellationToken);

    public Task<Conversation?> GetConversationByPublicIdAsync(
        Guid conversationId,
        CancellationToken cancellationToken) =>
        dbContext.Conversations.SingleOrDefaultAsync(
            conversation => conversation.PublicId == conversationId,
            cancellationToken);

    public Task<Message> GetLatestMessageAsync(
        long conversationId,
        CancellationToken cancellationToken) =>
        dbContext.Messages
            .Include(message => message.Conversation)
            .ThenInclude(conversation => conversation.Contact)
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.TelegramSentAt)
            .ThenByDescending(message => message.PublicId)
            .FirstAsync(cancellationToken);

    public void Add(Contact contact) => dbContext.Contacts.Add(contact);

    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await dbContext.SaveChangesAsync(cancellationToken);
}
