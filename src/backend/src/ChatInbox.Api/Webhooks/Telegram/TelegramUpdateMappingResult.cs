using ChatInbox.Contracts.IntegrationEvents;

namespace ChatInbox.Api.Webhooks.Telegram;

public enum TelegramUpdateMappingStatus
{
    Accepted,
    Ignored,
    Rejected
}

public sealed record TelegramUpdateMappingResult(
    TelegramUpdateMappingStatus Status,
    IncomingTextMessageIntegrationEvent? Message)
{
    public static TelegramUpdateMappingResult Accepted(IncomingTextMessageIntegrationEvent message) =>
        new(TelegramUpdateMappingStatus.Accepted, message);

    public static TelegramUpdateMappingResult Ignored() =>
        new(TelegramUpdateMappingStatus.Ignored, null);

    public static TelegramUpdateMappingResult Rejected() =>
        new(TelegramUpdateMappingStatus.Rejected, null);
}
