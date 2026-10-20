using ChatInbox.Contracts.Realtime;
using ChatInbox.Domain.Messages;

namespace ChatInbox.Application.Messages;

public static class MessageContractMapper
{
    public static MessageSummary ToSummary(Message message) =>
        new(
            message.PublicId,
            message.Text,
            message.Direction == MessageDirection.Incoming ? "incoming" : "outgoing",
            message.Status switch
            {
                MessageStatus.Received => "received",
                MessageStatus.Pending => "pending",
                MessageStatus.Sent => "sent",
                MessageStatus.Failed => "failed",
                _ => throw new ArgumentOutOfRangeException(nameof(message))
            },
            message.TelegramSentAt,
            message.ReceivedAt);
}
