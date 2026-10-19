namespace ChatInbox.Contracts.Realtime;

public sealed record MessageSummary(
    Guid Id,
    string Text,
    string Direction,
    DateTimeOffset TelegramSentAt,
    DateTimeOffset ReceivedAt);
