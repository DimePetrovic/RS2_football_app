namespace Comeback.Chat.Infrastructure.Persistence.Configurations;

using Comeback.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.SenderDisplayName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.EncryptedContent).IsRequired();
        builder.HasIndex(m => new { m.ConversationId, m.SentAt });
    }
}
