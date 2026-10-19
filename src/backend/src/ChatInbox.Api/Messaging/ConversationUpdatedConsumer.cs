using System.Text.Json;
using ChatInbox.Api.Hubs;
using ChatInbox.Contracts.IntegrationEvents;
using ChatInbox.Infrastructure.Messaging;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatInbox.Api.Messaging;

public sealed class ConversationUpdatedConsumer(
    IRabbitMqConnection rabbitMqConnection,
    IHubContext<InboxHub> hubContext,
    ILogger<ConversationUpdatedConsumer> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await rabbitMqConnection.GetConnectionAsync(stoppingToken);
        _channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.ExchangeDeclareAsync(
            RabbitMqTopology.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(
            RabbitMqTopology.ConversationUpdatesQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(
            RabbitMqTopology.ConversationUpdatesQueue,
            RabbitMqTopology.Exchange,
            RabbitMqTopology.ConversationUpdatedRoutingKey,
            cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(0, 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleAsync;

        await _channel.BasicConsumeAsync(
            RabbitMqTopology.ConversationUpdatesQueue,
            autoAck: false,
            consumer,
            stoppingToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task HandleAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_channel is null)
        {
            return;
        }

        try
        {
            var integrationEvent = JsonSerializer.Deserialize<ConversationUpdatedIntegrationEvent>(
                eventArgs.Body.Span,
                SerializerOptions);

            if (integrationEvent is null)
            {
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
                return;
            }

            await hubContext.Clients.All.SendAsync(
                InboxHubContract.ConversationUpdatedEvent,
                integrationEvent.Conversation);
            await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Invalid conversation update event received");
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Conversation update event processing failed");
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
