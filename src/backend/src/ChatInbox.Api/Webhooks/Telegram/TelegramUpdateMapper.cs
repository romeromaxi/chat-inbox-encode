using ChatInbox.Contracts.IntegrationEvents;
using ChatInbox.Domain.Messages;

namespace ChatInbox.Api.Webhooks.Telegram;

public sealed class TelegramUpdateMapper(TimeProvider timeProvider)
{
    public TelegramUpdateMappingResult Map(TelegramUpdateDto update)
    {
        if (update.EditedMessage is not null || update.Message is not { } message)
        {
            return TelegramUpdateMappingResult.Ignored();
        }

        if (message.Chat is not { Type: "private" } chat
            || message.From is not { } sender
            || message.Text is null)
        {
            return TelegramUpdateMappingResult.Ignored();
        }

        if (update.UpdateId < 0
            || message.MessageId <= 0
            || chat.Id <= 0
            || sender.Id <= 0
            || string.IsNullOrWhiteSpace(sender.FirstName)
            || message.Text.Length is 0 or > Message.MaximumTextLength)
        {
            return TelegramUpdateMappingResult.Rejected();
        }

        DateTimeOffset telegramSentAt;

        try
        {
            telegramSentAt = DateTimeOffset.FromUnixTimeSeconds(message.Date);
        }
        catch (ArgumentOutOfRangeException)
        {
            return TelegramUpdateMappingResult.Rejected();
        }

        return TelegramUpdateMappingResult.Accepted(
            new IncomingTextMessageIntegrationEvent(
                update.UpdateId,
                message.MessageId,
                chat.Id,
                sender.Id,
                sender.FirstName,
                sender.LastName,
                sender.Username,
                message.Text,
                telegramSentAt,
                timeProvider.GetUtcNow()));
    }
}
