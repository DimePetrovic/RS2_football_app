namespace Comeback.Social.Infrastructure.Persistence.Configurations;

using Comeback.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class UserFeedItemConfiguration : IEntityTypeConfiguration<UserFeedItem>
{
    public void Configure(EntityTypeBuilder<UserFeedItem> builder)
    {
        builder.ToTable("user_feed_items");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.HasIndex(f => new { f.UserId, f.PostId }).IsUnique();
        builder.HasIndex(f => new { f.UserId, f.CreatedAt });
    }
}
