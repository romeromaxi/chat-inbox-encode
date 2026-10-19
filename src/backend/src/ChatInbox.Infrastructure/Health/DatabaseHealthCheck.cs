using ChatInbox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ChatInbox.Infrastructure.Health;

public sealed class DatabaseHealthCheck(
    IDbContextFactory<ChatInboxDbContext> dbContextFactory)
    : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

        return canConnect
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("PostgreSQL is unavailable");
    }
}
