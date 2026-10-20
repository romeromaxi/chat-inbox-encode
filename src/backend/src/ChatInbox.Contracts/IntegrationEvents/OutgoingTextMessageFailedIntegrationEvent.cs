namespace ChatInbox.Contracts.IntegrationEvents;

public sealed record OutgoingTextMessageFailedIntegrationEvent(
    Guid MessageId,
    Guid ConversationId,
    long TelegramChatId,
    string Text,
    int Attempts,
    string Reason,
    DateTimeOffset FailedAt);
