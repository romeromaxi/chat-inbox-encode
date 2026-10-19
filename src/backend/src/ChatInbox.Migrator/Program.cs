using ChatInbox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Database")
    ?? throw new InvalidOperationException("ConnectionStrings__Database is required");

var options = new DbContextOptionsBuilder<ChatInboxDbContext>()
    .UseNpgsql(connectionString)
    .Options;

await using var dbContext = new ChatInboxDbContext(options);
await dbContext.Database.MigrateAsync();
