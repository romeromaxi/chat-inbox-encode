using ChatInbox.Domain.Contacts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatInbox.Infrastructure.Persistence.Configurations;

public sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("contacts");
        builder.HasKey(contact => contact.Id);
        builder.Property(contact => contact.Id).UseIdentityByDefaultColumn();
        builder.Property(contact => contact.PublicId).IsRequired();
        builder.Property(contact => contact.TelegramUserId).IsRequired();
        builder.Property(contact => contact.FirstName).HasMaxLength(255).IsRequired();
        builder.Property(contact => contact.LastName).HasMaxLength(255);
        builder.Property(contact => contact.Username).HasMaxLength(255);
        builder.HasIndex(contact => contact.PublicId)
            .IsUnique()
            .HasDatabaseName("ux_contacts_public_id");
        builder.HasIndex(contact => contact.TelegramUserId)
            .IsUnique()
            .HasDatabaseName("ux_contacts_telegram_user_id");
        builder.HasOne(contact => contact.Conversation)
            .WithOne(conversation => conversation.Contact)
            .HasForeignKey<Domain.Conversations.Conversation>(conversation => conversation.ContactId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
