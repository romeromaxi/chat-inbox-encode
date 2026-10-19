using ChatInbox.Domain.Conversations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatInbox.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations");
        builder.HasKey(conversation => conversation.Id);
        builder.Property(conversation => conversation.Id).UseIdentityByDefaultColumn();
        builder.Property(conversation => conversation.PublicId).IsRequired();
        builder.Property(conversation => conversation.TelegramChatId).IsRequired();
        builder.Property(conversation => conversation.UnreadCount).IsRequired();
        builder.Property(conversation => conversation.LastMessageAt)
            .HasColumnType("timestamp with time zone");
        builder.HasIndex(conversation => conversation.PublicId)
            .IsUnique()
            .HasDatabaseName("ux_conversations_public_id");
        builder.HasIndex(conversation => conversation.ContactId)
            .IsUnique()
            .HasDatabaseName("ux_conversations_contact_id");
        builder.HasIndex(conversation => conversation.TelegramChatId)
            .IsUnique()
            .HasDatabaseName("ux_conversations_telegram_chat_id");
        builder.HasIndex(conversation => conversation.LastMessageAt)
            .HasDatabaseName("ix_conversations_last_message_at");
        builder.Navigation(conversation => conversation.Messages)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
