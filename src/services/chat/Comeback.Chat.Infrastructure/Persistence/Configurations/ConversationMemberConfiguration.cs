namespace Comeback.Chat.Infrastructure.Persistence.Configurations;

using Comeback.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ConversationMemberConfiguration : IEntityTypeConfiguration<ConversationMember>
{
    public void Configure(EntityTypeBuilder<ConversationMember> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(m => m.LastReadAt).IsRequired(false);
        builder.Property(m => m.ClearedAt).IsRequired(false);
        builder.HasIndex(m => new { m.ConversationId, m.UserId }).IsUnique();
        builder.HasIndex(m => m.UserId);
    }
}
