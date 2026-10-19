using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ChatInbox.Infrastructure.Messaging;

public sealed class RabbitMqConnection(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnection> logger)
    : IRabbitMqConnection, IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            var settings = options.Value;
            var factory = new ConnectionFactory
            {
                HostName = settings.Host,
                Port = settings.Port,
                UserName = settings.Username,
                Password = settings.Password,
                AutomaticRecoveryEnabled = true
            };

            _connection = await factory.CreateConnectionAsync(cancellationToken);
            logger.LogInformation("RabbitMQ connection established");

            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _gate.Dispose();
    }
}
