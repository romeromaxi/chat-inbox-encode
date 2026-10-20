using ChatInbox.Domain.Contacts;
using ChatInbox.Domain.Messages;

namespace ChatInbox.Domain.Conversations;

public sealed class Conversation
{
    private readonly List<Message> _messages = [];

    private Conversation()
    {
    }

    private Conversation(Contact contact, long telegramChatId)
    {
        PublicId = Guid.NewGuid();
        Contact = contact;
        TelegramChatId = telegramChatId;
    }

    public long Id { get; private set; }
    public Guid PublicId { get; private set; }
    public long ContactId { get; private set; }
    public Contact Contact { get; private set; } = null!;
    public long TelegramChatId { get; private set; }
    public int UnreadCount { get; private set; }
    public DateTimeOffset? LastMessageAt { get; private set; }
    public IReadOnlyCollection<Message> Messages => _messages;

    internal static Conversation Create(Contact contact, long telegramChatId)
    {
        ArgumentNullException.ThrowIfNull(contact);

        if (telegramChatId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramChatId));
        }

        return new Conversation(contact, telegramChatId);
    }

    public Message ReceiveMessage(
        long telegramUpdateId,
        long telegramMessageId,
        string text,
        DateTimeOffset telegramSentAt,
        DateTimeOffset receivedAt)
    {
        var message = Message.CreateIncoming(
            this,
            telegramUpdateId,
            telegramMessageId,
            text,
            telegramSentAt,
            receivedAt);

        _messages.Add(message);
        UnreadCount = checked(UnreadCount + 1);

        if (LastMessageAt is null || telegramSentAt > LastMessageAt)
        {
            LastMessageAt = telegramSentAt;
        }

        return message;
    }

    public void MarkAsRead() => UnreadCount = 0;

    public Message SendMessage(string text, DateTimeOffset createdAt)
    {
        var message = Message.CreateOutgoing(this, text, createdAt);
        _messages.Add(message);
        LastMessageAt = createdAt;
        return message;
    }
}
