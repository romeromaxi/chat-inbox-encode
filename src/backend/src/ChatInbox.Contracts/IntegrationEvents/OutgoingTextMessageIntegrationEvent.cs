namespace ChatInbox.Contracts.IntegrationEvents;

public sealed record OutgoingTextMessageIntegrationEvent(
    Guid MessageId,
    Guid ConversationId,
    long TelegramChatId,
    string Text);
