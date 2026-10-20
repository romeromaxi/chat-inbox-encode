using ChatInbox.Domain.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatInbox.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).UseIdentityByDefaultColumn();
        builder.Property(message => message.PublicId).IsRequired();
        builder.Property(message => message.TelegramUpdateId);
        builder.Property(message => message.TelegramMessageId);
        builder.Property(message => message.Text)
            .HasMaxLength(Message.MaximumTextLength)
            .IsRequired();
        builder.Property(message => message.Direction)
            .HasConversion<short>()
            .IsRequired();
        builder.Property(message => message.Status)
            .HasConversion<short>()
            .IsRequired();
        builder.Property(message => message.TelegramSentAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(message => message.ReceivedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.HasIndex(message => message.PublicId)
            .IsUnique()
            .HasDatabaseName("ux_messages_public_id");
        builder.HasIndex(message => message.TelegramUpdateId)
            .IsUnique()
            .HasDatabaseName("ux_messages_telegram_update_id");
        builder.HasIndex(message => new { message.ConversationId, message.TelegramMessageId })
            .IsUnique()
            .HasDatabaseName("ux_messages_conversation_telegram_message_id");
        builder.HasIndex(message => new { message.ConversationId, message.TelegramSentAt })
            .HasDatabaseName("ix_messages_conversation_telegram_sent_at");
        builder.HasOne(message => message.Conversation)
            .WithMany(conversation => conversation.Messages)
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
