namespace ChatInbox.Infrastructure.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TMessage>(
        TMessage message,
        string routingKey,
        string messageId,
        CancellationToken cancellationToken);
}
