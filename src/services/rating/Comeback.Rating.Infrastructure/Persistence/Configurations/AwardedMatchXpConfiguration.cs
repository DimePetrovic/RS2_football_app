namespace Comeback.Rating.Infrastructure.Persistence.Configurations;

using Comeback.Rating.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class AwardedMatchXpConfiguration : IEntityTypeConfiguration<AwardedMatchXp>
{
    public void Configure(EntityTypeBuilder<AwardedMatchXp> builder)
    {
        builder.ToTable("awarded_match_xp");

        // Composite key enforces idempotency at the database level.
        builder.HasKey(a => new { a.MatchId, a.UserId });

        builder.Property(a => a.MatchId)
            .HasColumnName("match_id")
            .IsRequired();

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(a => a.AwardedAt)
            .HasColumnName("awarded_at")
            .IsRequired();
    }
}
