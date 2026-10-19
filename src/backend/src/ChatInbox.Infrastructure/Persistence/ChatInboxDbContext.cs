using ChatInbox.Domain.Contacts;
using ChatInbox.Domain.Conversations;
using ChatInbox.Domain.Messages;
using Microsoft.EntityFrameworkCore;

namespace ChatInbox.Infrastructure.Persistence;

public sealed class ChatInboxDbContext(DbContextOptions<ChatInboxDbContext> options)
    : DbContext(options)
{
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChatInboxDbContext).Assembly);
    }
}
