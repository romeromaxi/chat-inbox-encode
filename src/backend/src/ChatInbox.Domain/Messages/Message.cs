using ChatInbox.Domain.Conversations;

namespace ChatInbox.Domain.Messages;

public sealed class Message
{
    public const int MaximumTextLength = 4096;

    private Message()
    {
    }

    private Message(
        Conversation conversation,
        long? telegramUpdateId,
        long? telegramMessageId,
        string text,
        MessageDirection direction,
        MessageStatus status,
        DateTimeOffset telegramSentAt,
        DateTimeOffset receivedAt)
    {
        PublicId = Guid.NewGuid();
        Conversation = conversation;
        TelegramUpdateId = telegramUpdateId;
        TelegramMessageId = telegramMessageId;
        Text = text;
        Direction = direction;
        Status = status;
        TelegramSentAt = telegramSentAt;
        ReceivedAt = receivedAt;
    }

    public long Id { get; private set; }
    public Guid PublicId { get; private set; }
    public long ConversationId { get; private set; }
    public Conversation Conversation { get; private set; } = null!;
    public long? TelegramUpdateId { get; private set; }
    public long? TelegramMessageId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public MessageDirection Direction { get; private set; }
    public MessageStatus Status { get; private set; }
    public DateTimeOffset TelegramSentAt { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }

    internal static Message CreateIncoming(
        Conversation conversation,
        long telegramUpdateId,
        long telegramMessageId,
        string text,
        DateTimeOffset telegramSentAt,
        DateTimeOffset receivedAt)
    {
        ArgumentNullException.ThrowIfNull(conversation);

        if (telegramUpdateId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramUpdateId));
        }

        if (telegramMessageId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramMessageId));
        }

        ArgumentException.ThrowIfNullOrEmpty(text);

        if (text.Length > MaximumTextLength)
        {
            throw new ArgumentOutOfRangeException(nameof(text));
        }

        return new Message(
            conversation,
            telegramUpdateId,
            telegramMessageId,
            text,
            MessageDirection.Incoming,
            MessageStatus.Received,
            telegramSentAt,
            receivedAt);
    }

    internal static Message CreateOutgoing(
        Conversation conversation,
        string text,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ValidateText(text);

        return new Message(
            conversation,
            null,
            null,
            text,
            MessageDirection.Outgoing,
            MessageStatus.Pending,
            createdAt,
            createdAt);
    }

    public void MarkSent(long telegramMessageId, DateTimeOffset telegramSentAt)
    {
        if (Direction != MessageDirection.Outgoing || Status != MessageStatus.Pending)
        {
            throw new InvalidOperationException("Only pending outgoing messages can be marked as sent");
        }

        if (telegramMessageId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramMessageId));
        }

        TelegramMessageId = telegramMessageId;
        TelegramSentAt = telegramSentAt;
        Status = MessageStatus.Sent;
    }

    public void MarkFailed()
    {
        if (Direction != MessageDirection.Outgoing || Status != MessageStatus.Pending)
        {
            throw new InvalidOperationException("Only pending outgoing messages can be marked as failed");
        }

        Status = MessageStatus.Failed;
    }

    private static void ValidateText(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        if (text.Length > MaximumTextLength)
        {
            throw new ArgumentOutOfRangeException(nameof(text));
        }
    }
}
