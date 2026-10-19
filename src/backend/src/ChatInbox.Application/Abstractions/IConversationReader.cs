using ChatInbox.Contracts.Inbox;
using ChatInbox.Contracts.Realtime;

namespace ChatInbox.Application.Abstractions;

public interface IConversationReader
{
    Task<IReadOnlyList<ConversationSummary>> GetConversationsAsync(
        bool unreadOnly,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MessageSummary>?> GetMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken);
}
