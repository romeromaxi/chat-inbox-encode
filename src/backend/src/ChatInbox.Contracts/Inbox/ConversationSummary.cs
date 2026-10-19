using ChatInbox.Contracts.Realtime;

namespace ChatInbox.Contracts.Inbox;

public sealed record ConversationSummary(
    Guid ConversationId,
    ContactSummary Contact,
    MessageSummary LastMessage,
    int UnreadCount,
    DateTimeOffset LastMessageAt);
