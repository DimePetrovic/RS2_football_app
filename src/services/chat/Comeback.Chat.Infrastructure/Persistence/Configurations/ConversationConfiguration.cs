namespace Comeback.Chat.Infrastructure.Persistence.Configurations;

using Comeback.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Type).HasConversion<int>();
        builder.Property(c => c.GroupId).IsRequired(false);
        builder.Property(c => c.Title).HasMaxLength(200).IsRequired(false);
        builder.Property(c => c.GroupAvatarUrl).HasMaxLength(500).IsRequired(false);

        // At most one conversation per group.
        builder.HasIndex(c => c.GroupId).IsUnique().HasFilter("\"GroupId\" IS NOT NULL");

        builder.HasMany<ConversationMember>(c => c.Members).WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Members).HasField("_members").UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasMany<Message>(c => c.Messages).WithOne().HasForeignKey(m => m.ConversationId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(c => c.Messages).HasField("_messages").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
