namespace Comeback.Profile.Infrastructure.Persistence.Configurations;

using Comeback.Profile.Domain.Entities;
using Comeback.Profile.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("profiles");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.Username)
            .HasColumnName("username")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(p => p.Email)
            .HasColumnName("email")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(p => p.Nationality)
            .HasMaxLength(2)
            .IsRequired(false);

        builder.Property(p => p.FirstName)
            .HasColumnName("first_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.LastName)
            .HasColumnName("last_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.DateOfBirth)
            .HasColumnName("date_of_birth")
            .IsRequired();

        builder.Property(p => p.PreferredPosition)
            .HasColumnName("preferred_position")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.CanPlayGoalkeeper)
            .HasColumnName("can_play_goalkeeper")
            .IsRequired();

        builder.Property(p => p.YouthSeasons)
            .HasColumnName("youth_seasons")
            .IsRequired();

        builder.Property(p => p.SeniorSeasons)
            .HasColumnName("senior_seasons")
            .IsRequired();

        builder.Property(p => p.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(100);

        builder.Property(p => p.Bio)
            .HasColumnName("bio")
            .HasMaxLength(500);

        builder.Property(p => p.AvatarUrl)
            .HasColumnName("avatar_url")
            .HasMaxLength(2048);

        builder.Property(p => p.SkillLevel)
            .HasColumnName("skill_level")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Role)
            .HasColumnName("role")
            .HasMaxLength(20)
            .HasDefaultValue("Player")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Ignore(p => p.DomainEvents);
    }
}
