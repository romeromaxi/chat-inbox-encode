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

public sealed class OutgoingTextMessageConsumer(
    IRabbitMqConnection rabbitMqConnection,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<OutgoingTextMessageConsumer> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];
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
        await DeclareQueueAsync(
            RabbitMqTopology.OutgoingTextQueue,
            RabbitMqTopology.OutgoingTextRoutingKey,
            stoppingToken);
        await DeclareQueueAsync(
            RabbitMqTopology.OutgoingTextDeadLetterQueue,
            RabbitMqTopology.OutgoingTextFailedRoutingKey,
            stoppingToken);
        await _channel.BasicQosAsync(0, 1, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleAsync;

        await _channel.BasicConsumeAsync(
            RabbitMqTopology.OutgoingTextQueue,
            autoAck: false,
            consumer,
            stoppingToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task DeclareQueueAsync(
        string queue,
        string routingKey,
        CancellationToken cancellationToken)
    {
        await _channel!.QueueDeclareAsync(
            queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(
            queue,
            RabbitMqTopology.Exchange,
            routingKey,
            cancellationToken: cancellationToken);
    }

    private async Task HandleAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        if (_channel is null)
        {
            return;
        }

        OutgoingTextMessageIntegrationEvent? outgoingMessage;

        try
        {
            outgoingMessage = JsonSerializer.Deserialize<OutgoingTextMessageIntegrationEvent>(
                eventArgs.Body.Span,
                SerializerOptions);
        }
        catch (JsonException exception)
        {
            logger.LogError(exception, "Invalid outgoing Telegram event received");
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
            return;
        }

        if (outgoingMessage is null)
        {
            await _channel.BasicNackAsync(eventArgs.DeliveryTag, false, false);
            return;
        }

        Exception? lastException = null;

        for (var attempt = 0; attempt <= RetryDelays.Length; attempt++)
        {
            try
            {
                await ProcessAsync(outgoingMessage, eventArgs.CancellationToken);
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
                return;
            }
            catch (OperationCanceledException) when (eventArgs.CancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastException = exception;
                logger.LogWarning(
                    exception,
                    "Telegram send attempt {Attempt} of {TotalAttempts} failed for message {MessageId}",
                    attempt + 1,
                    RetryDelays.Length + 1,
                    outgoingMessage.MessageId);

                if (attempt < RetryDelays.Length)
                {
                    await Task.Delay(AddJitter(RetryDelays[attempt]), eventArgs.CancellationToken);
                }
            }
        }

        await MarkFailedAndDeadLetterAsync(
            outgoingMessage,
            lastException ?? new InvalidOperationException("Telegram send failed"),
            eventArgs.CancellationToken);
        await _channel.BasicAckAsync(eventArgs.DeliveryTag, false);
    }

    private async Task ProcessAsync(
        OutgoingTextMessageIntegrationEvent outgoingMessage,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IProcessOutgoingMessageHandler>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var update = await handler.SendAsync(outgoingMessage, cancellationToken);

        if (update is not null)
        {
            await publisher.PublishAsync(
                update,
                RabbitMqTopology.ConversationUpdatedRoutingKey,
                outgoingMessage.MessageId.ToString(),
                cancellationToken);
        }
    }

    private async Task MarkFailedAndDeadLetterAsync(
        OutgoingTextMessageIntegrationEvent outgoingMessage,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var handler = scope.ServiceProvider.GetRequiredService<IProcessOutgoingMessageHandler>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        var update = await handler.MarkFailedAsync(outgoingMessage.MessageId, cancellationToken);

        if (update is not null)
        {
            await publisher.PublishAsync(
                update,
                RabbitMqTopology.ConversationUpdatedRoutingKey,
                outgoingMessage.MessageId.ToString(),
                cancellationToken);
        }

        var reason = exception.Message.Length <= 1000
            ? exception.Message
            : exception.Message[..1000];
        var failed = new OutgoingTextMessageFailedIntegrationEvent(
            outgoingMessage.MessageId,
            outgoingMessage.ConversationId,
            outgoingMessage.TelegramChatId,
            outgoingMessage.Text,
            RetryDelays.Length + 1,
            reason,
            timeProvider.GetUtcNow());

        await publisher.PublishAsync(
            failed,
            RabbitMqTopology.OutgoingTextFailedRoutingKey,
            outgoingMessage.MessageId.ToString(),
            cancellationToken);
    }

    private static TimeSpan AddJitter(TimeSpan delay)
    {
        const double jitterRange = 0.2;
        var factor = 1 - jitterRange + (Random.Shared.NextDouble() * jitterRange * 2);
        return TimeSpan.FromMilliseconds(delay.TotalMilliseconds * factor);
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
