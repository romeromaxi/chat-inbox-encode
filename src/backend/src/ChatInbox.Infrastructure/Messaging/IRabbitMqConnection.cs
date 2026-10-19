using RabbitMQ.Client;

namespace ChatInbox.Infrastructure.Messaging;

public interface IRabbitMqConnection
{
    Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken);
}
