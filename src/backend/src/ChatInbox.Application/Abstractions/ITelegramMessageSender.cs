namespace ChatInbox.Application.Abstractions;

public interface ITelegramMessageSender
{
    Task<TelegramSentMessage> SendTextMessageAsync(
        long chatId,
        string text,
        CancellationToken cancellationToken);
}

public sealed record TelegramSentMessage(long MessageId, DateTimeOffset SentAt);
