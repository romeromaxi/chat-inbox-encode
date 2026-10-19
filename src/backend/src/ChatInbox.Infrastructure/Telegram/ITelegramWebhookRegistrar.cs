namespace ChatInbox.Infrastructure.Telegram;

public interface ITelegramWebhookRegistrar
{
    Task RegisterAsync(CancellationToken cancellationToken);
}
