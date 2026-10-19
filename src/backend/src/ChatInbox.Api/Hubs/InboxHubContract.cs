namespace ChatInbox.Api.Hubs;

public static class InboxHubContract
{
    public const string Path = "/hubs/inbox";
    public const string ConversationUpdatedEvent = "conversationUpdated";
}
