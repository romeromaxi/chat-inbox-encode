using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ChatInbox.Infrastructure.Persistence;

public sealed class ChatInboxDbContextFactory : IDesignTimeDbContextFactory<ChatInboxDbContext>
{
    public ChatInboxDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
            ?? throw new InvalidOperationException("ConnectionStrings__Database is required");

        var options = new DbContextOptionsBuilder<ChatInboxDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ChatInboxDbContext(options);
    }
}
