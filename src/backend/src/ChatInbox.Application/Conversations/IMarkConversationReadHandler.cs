namespace ChatInbox.Application.Conversations;

public interface IMarkConversationReadHandler
{
    Task<bool> HandleAsync(Guid conversationId, CancellationToken cancellationToken);
}
