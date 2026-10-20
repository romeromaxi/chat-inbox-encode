namespace ChatInbox.Contracts.Realtime;

public sealed record MessageSummary(
    Guid Id,
    string Text,
    string Direction,
    string Status,
    DateTimeOffset TelegramSentAt,
    DateTimeOffset ReceivedAt);
