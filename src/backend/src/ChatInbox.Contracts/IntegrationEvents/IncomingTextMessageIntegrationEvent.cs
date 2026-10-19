namespace ChatInbox.Contracts.IntegrationEvents;

public sealed record IncomingTextMessageIntegrationEvent(
    long UpdateId,
    long MessageId,
    long ChatId,
    long UserId,
    string FirstName,
    string? LastName,
    string? Username,
    string Text,
    DateTimeOffset TelegramSentAt,
    DateTimeOffset ReceivedAt);
