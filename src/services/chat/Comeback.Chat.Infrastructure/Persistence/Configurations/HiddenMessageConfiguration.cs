namespace Comeback.Chat.Infrastructure.Persistence.Configurations;

using Comeback.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class HiddenMessageConfiguration : IEntityTypeConfiguration<HiddenMessage>
{
    public void Configure(EntityTypeBuilder<HiddenMessage> builder)
    {
        builder.HasKey(h => h.Id);
        builder.HasIndex(h => new { h.UserId, h.MessageId }).IsUnique();
        builder.HasIndex(h => h.UserId);
    }
}
