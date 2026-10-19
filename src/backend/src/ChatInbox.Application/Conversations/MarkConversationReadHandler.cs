using ChatInbox.Application.Abstractions;

namespace ChatInbox.Application.Conversations;

public sealed class MarkConversationReadHandler(IInboxRepository repository)
    : IMarkConversationReadHandler
{
    public async Task<bool> HandleAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await repository.GetConversationByPublicIdAsync(
            conversationId,
            cancellationToken);

        if (conversation is null)
        {
            return false;
        }

        conversation.MarkAsRead();
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
