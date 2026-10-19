namespace ChatInbox.Contracts.Realtime;

public sealed record ConversationUpdatedEvent(
    Guid ConversationId,
    ContactSummary Contact,
    MessageSummary LastMessage,
    int UnreadCount,
    DateTimeOffset LastMessageAt);
