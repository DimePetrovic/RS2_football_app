namespace Comeback.Profile.Infrastructure.Persistence.Configurations;

using Comeback.Profile.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PlayerFollowConfiguration : IEntityTypeConfiguration<PlayerFollow>
{
    public void Configure(EntityTypeBuilder<PlayerFollow> builder)
    {
        builder.ToTable("player_follows");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id");

        builder.Property(f => f.FollowerUserId).HasColumnName("follower_user_id").IsRequired();
        builder.Property(f => f.FollowedUserId).HasColumnName("followed_user_id").IsRequired();
        builder.Property(f => f.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.HasIndex(f => new { f.FollowerUserId, f.FollowedUserId }).IsUnique();
        builder.HasIndex(f => f.FollowedUserId);
    }
}
