namespace ChatInbox.Contracts.IntegrationEvents;

public static class RabbitMqTopology
{
    public const string Exchange = "chat-inbox";
    public const string IncomingTextQueue = "chat-inbox.telegram.incoming-text";
    public const string IncomingTextRoutingKey = "telegram.message.received";
    public const string ConversationUpdatesQueue = "chat-inbox.api.conversation-updates";
    public const string ConversationUpdatedRoutingKey = "conversation.message.received";
}
