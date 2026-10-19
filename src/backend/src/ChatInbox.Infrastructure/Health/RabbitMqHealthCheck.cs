using ChatInbox.Infrastructure.Messaging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChatInbox.Infrastructure.Health;

public sealed class RabbitMqHealthCheck(IRabbitMqConnection connection) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var rabbitMqConnection = await connection.GetConnectionAsync(cancellationToken);

        return rabbitMqConnection.IsOpen
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("RabbitMQ is unavailable");
    }
}
