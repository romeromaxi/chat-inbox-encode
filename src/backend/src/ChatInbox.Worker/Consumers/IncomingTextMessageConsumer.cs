using System.Text.Json;
using ChatInbox.Application.Messages;
using ChatInbox.Contracts.IntegrationEvents;
using ChatInbox.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ChatInbox.Worker.Consumers;

public sealed class IncomingTextMessageConsumer(
    IRabbitMqConnection rabbitMqConnection,
    IServiceScopeFactory scopeFactory,
    ILogger<IncomingTextMessageConsumer> logger)
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
            RabbitMqTopology.IncomingTextQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);
        await _channel.QueueBindAsync(
            RabbitMqTopology.IncomingTextQueue,
            RabbitMqTopology.Exchange,
            RabbitMqTopology.IncomingTextRoutingKey,
            cancellationToken: stoppingToken);
        await _channel.BasicQosAsync(0, 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleAsync;

        await _channel.BasicConsumeAsync(
            RabbitMqTopology.IncomingTextQueue,
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
            var incomingMessage = JsonSerializer.Deserialize<IncomingTextMessageIntegrationEvent>(
                eventArgs.Body.Span,
                SerializerOptions);

            if (incomingMessage is null)
            {
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
                return;
            }

            await ProcessAndPublishAsync(incomingMessage, eventArgs.CancellationToken);
            await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Invalid incoming Telegram event received");
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Incoming Telegram event processing failed");
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, true);
        }
    }

    private async Task ProcessAndPublishAsync(
        IncomingTextMessageIntegrationEvent incomingMessage,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IIncomingTextMessageHandler>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var conversationUpdated = await handler.HandleAsync(incomingMessage, cancellationToken);
        await publisher.PublishAsync(
            conversationUpdated,
            RabbitMqTopology.ConversationUpdatedRoutingKey,
            conversationUpdated.Conversation.LastMessage.Id.ToString(),
            cancellationToken);
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
