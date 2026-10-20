namespace ChatInbox.Contracts.IntegrationEvents;

public static class RabbitMqTopology
{
    public const string Exchange = "chat-inbox";
    public const string IncomingTextQueue = "chat-inbox.telegram.incoming-text";
    public const string IncomingTextRoutingKey = "telegram.message.received";
    public const string OutgoingTextQueue = "chat-inbox.telegram.outgoing-text";
    public const string OutgoingTextRoutingKey = "telegram.message.send";
    public const string OutgoingTextDeadLetterQueue = "chat-inbox.telegram.outgoing-text.failed";
    public const string OutgoingTextFailedRoutingKey = "telegram.message.failed";
    public const string ConversationUpdatesQueue = "chat-inbox.api.conversation-updates";
    public const string ConversationUpdatedRoutingKey = "conversation.updated";
}
