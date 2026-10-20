namespace ChatInbox.Application.Messages;

public interface IQueueOutgoingMessageHandler
{
    Task<QueueOutgoingMessageResult?> HandleAsync(
        Guid conversationId,
        string text,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken);
}
