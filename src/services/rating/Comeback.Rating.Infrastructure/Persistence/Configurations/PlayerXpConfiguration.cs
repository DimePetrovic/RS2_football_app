namespace Comeback.Rating.Infrastructure.Persistence.Configurations;

using Comeback.Rating.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PlayerXpConfiguration : IEntityTypeConfiguration<PlayerXp>
{
    public void Configure(EntityTypeBuilder<PlayerXp> builder)
    {
        builder.ToTable("player_xp");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.CareerXp)
            .HasColumnName("career_xp")
            .IsRequired();

        builder.Property(p => p.MatchXp)
            .HasColumnName("match_xp")
            .IsRequired();

        builder.Property(p => p.YouthSeasons)
            .HasColumnName("youth_seasons")
            .IsRequired();

        builder.Property(p => p.SeniorSeasons)
            .HasColumnName("senior_seasons")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        // Computed properties not mapped to columns
        builder.Ignore(p => p.TotalXp);
        builder.Ignore(p => p.Level);
        builder.Ignore(p => p.DomainEvents);
    }
}
