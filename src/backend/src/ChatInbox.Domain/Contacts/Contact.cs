using ChatInbox.Domain.Conversations;

namespace ChatInbox.Domain.Contacts;

public sealed class Contact
{
    private Contact()
    {
    }

    private Contact(long telegramUserId, string firstName, string? lastName, string? username)
    {
        PublicId = Guid.NewGuid();
        TelegramUserId = telegramUserId;
        UpdateProfile(firstName, lastName, username);
    }

    public long Id { get; private set; }
    public Guid PublicId { get; private set; }
    public long TelegramUserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string? LastName { get; private set; }
    public string? Username { get; private set; }
    public Conversation? Conversation { get; private set; }

    public static Contact Create(
        long telegramUserId,
        string firstName,
        string? lastName,
        string? username)
    {
        if (telegramUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramUserId));
        }

        return new Contact(telegramUserId, firstName, lastName, username);
    }

    public void UpdateProfile(string firstName, string? lastName, string? username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);

        FirstName = firstName;
        LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName;
        Username = string.IsNullOrWhiteSpace(username) ? null : username;
    }

    public Conversation StartConversation(long telegramChatId)
    {
        if (Conversation is not null)
        {
            return Conversation;
        }

        Conversation = Conversation.Create(this, telegramChatId);
        return Conversation;
    }
}
