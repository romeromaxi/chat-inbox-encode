using System.Text.Json;
using ChatInbox.Contracts.IntegrationEvents;
using RabbitMQ.Client;

namespace ChatInbox.Infrastructure.Messaging;

public sealed class RabbitMqIntegrationEventPublisher(IRabbitMqConnection rabbitMqConnection)
    : IIntegrationEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishAsync<TMessage>(
        TMessage message,
        string routingKey,
        string messageId,
        CancellationToken cancellationToken)
    {
        var connection = await rabbitMqConnection.GetConnectionAsync(cancellationToken);
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        await using var channel = await connection.CreateChannelAsync(channelOptions, cancellationToken);
        await channel.ExchangeDeclareAsync(
            RabbitMqTopology.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var queue = routingKey switch
        {
            RabbitMqTopology.IncomingTextRoutingKey => RabbitMqTopology.IncomingTextQueue,
            RabbitMqTopology.OutgoingTextRoutingKey => RabbitMqTopology.OutgoingTextQueue,
            RabbitMqTopology.OutgoingTextFailedRoutingKey => RabbitMqTopology.OutgoingTextDeadLetterQueue,
            RabbitMqTopology.ConversationUpdatedRoutingKey => RabbitMqTopology.ConversationUpdatesQueue,
            _ => throw new ArgumentOutOfRangeException(nameof(routingKey))
        };

        await channel.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue,
            RabbitMqTopology.Exchange,
            routingKey,
            cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = messageId
        };

        await channel.BasicPublishAsync(
            RabbitMqTopology.Exchange,
            routingKey,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}
