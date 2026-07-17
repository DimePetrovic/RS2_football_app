namespace Comeback.Profile.Infrastructure.Persistence.Configurations;

using Comeback.Profile.Domain.Entities;
using Comeback.Profile.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PlayerGroupMemberConfiguration : IEntityTypeConfiguration<PlayerGroupMember>
{
    public void Configure(EntityTypeBuilder<PlayerGroupMember> builder)
    {
        builder.ToTable("player_group_members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.GroupId)
            .HasColumnName("group_id")
            .IsRequired();

        builder.Property(m => m.ProfileId)
            .HasColumnName("profile_id")
            .IsRequired();

        builder.HasIndex(m => new { m.GroupId, m.ProfileId })
            .IsUnique();

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.JoinedAt)
            .HasColumnName("joined_at")
            .IsRequired();
    }
}
