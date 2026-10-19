namespace ChatInbox.Infrastructure.Telegram;

public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    public string BotToken { get; init; } = string.Empty;
    public string WebhookSecretToken { get; init; } = string.Empty;
    public string WebhookUrl { get; init; } = string.Empty;
    public bool PrivateChatsOnly { get; init; } = true;
}
