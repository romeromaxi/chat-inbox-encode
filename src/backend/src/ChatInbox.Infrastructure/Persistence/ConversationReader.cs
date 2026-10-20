using ChatInbox.Application.Abstractions;
using ChatInbox.Contracts.Inbox;
using ChatInbox.Contracts.Realtime;
using ChatInbox.Domain.Messages;
using Microsoft.EntityFrameworkCore;

namespace ChatInbox.Infrastructure.Persistence;

public sealed class ConversationReader(IDbContextFactory<ChatInboxDbContext> dbContextFactory)
    : IConversationReader
{
    public async Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        bool unreadOnly,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = dbContext.Conversations
            .AsNoTracking()
            .Where(conversation => conversation.LastMessageAt != null);

        if (unreadOnly)
        {
            query = query.Where(conversation => conversation.UnreadCount > 0);
        }

        var conversations = await query
            .OrderByDescending(conversation => conversation.LastMessageAt)
            .ThenBy(conversation => conversation.PublicId)
            .Select(conversation => new
            {
                conversation.PublicId,
                ContactId = conversation.Contact.PublicId,
                conversation.Contact.FirstName,
                conversation.Contact.LastName,
                conversation.Contact.Username,
                conversation.UnreadCount,
                LastMessageAt = conversation.LastMessageAt!.Value,
                LastMessage = conversation.Messages
                    .OrderByDescending(message => message.TelegramSentAt)
                    .ThenByDescending(message => message.Id)
                    .Select(message => new
                    {
                        message.PublicId,
                        message.Text,
                        message.Direction,
                        message.Status,
                        message.TelegramSentAt,
                        message.ReceivedAt
                    })
                    .First()
            })
            .ToListAsync(cancellationToken);

        return conversations
            .Select(conversation => new ConversationSummary(
                conversation.PublicId,
                new ContactSummary(
                    conversation.ContactId,
                    conversation.FirstName,
                    conversation.LastName,
                    conversation.Username),
                new MessageSummary(
                    conversation.LastMessage.PublicId,
                    conversation.LastMessage.Text,
                    ToContractDirection(conversation.LastMessage.Direction),
                    ToContractStatus(conversation.LastMessage.Status),
                    conversation.LastMessage.TelegramSentAt,
                    conversation.LastMessage.ReceivedAt),
                conversation.UnreadCount,
                conversation.LastMessageAt))
            .ToArray();
    }

    public async Task<IReadOnlyList<MessageSummary>?> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var exists = await dbContext.Conversations
            .AsNoTracking()
            .AnyAsync(
                conversation => conversation.PublicId == conversationId,
                cancellationToken);

        if (!exists)
        {
            return null;
        }

        var messages = await dbContext.Messages
            .AsNoTracking()
            .Where(message => message.Conversation.PublicId == conversationId)
            .OrderBy(message => message.TelegramSentAt)
            .ThenBy(message => message.Id)
            .Select(message => new
            {
                message.PublicId,
                message.Text,
                message.Direction,
                message.Status,
                message.TelegramSentAt,
                message.ReceivedAt
            })
            .ToListAsync(cancellationToken);

        return messages
            .Select(message => new MessageSummary(
                message.PublicId,
                message.Text,
                ToContractDirection(message.Direction),
                ToContractStatus(message.Status),
                message.TelegramSentAt,
                message.ReceivedAt))
            .ToArray();
    }

    private static string ToContractDirection(MessageDirection direction) =>
        direction switch
        {
            MessageDirection.Incoming => "incoming",
            MessageDirection.Outgoing => "outgoing",
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };

    private static string ToContractStatus(MessageStatus status) =>
        status switch
        {
            MessageStatus.Received => "received",
            MessageStatus.Pending => "pending",
            MessageStatus.Sent => "sent",
            MessageStatus.Failed => "failed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };
}
