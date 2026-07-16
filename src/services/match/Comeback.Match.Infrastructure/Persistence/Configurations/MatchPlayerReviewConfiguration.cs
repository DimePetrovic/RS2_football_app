namespace Comeback.Match.Infrastructure.Persistence.Configurations;

using Comeback.Match.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MatchPlayerReviewConfiguration : IEntityTypeConfiguration<MatchPlayerReview>
{
    public void Configure(EntityTypeBuilder<MatchPlayerReview> builder)
    {
        builder.ToTable("match_player_reviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.OverallRating).HasPrecision(3, 1);
        builder.Property(r => r.GoalkeepingRating).HasPrecision(3, 1);
        builder.Property(r => r.DefenseRating).HasPrecision(3, 1);
        builder.Property(r => r.AttackRating).HasPrecision(3, 1);
        builder.Property(r => r.EffortRating).HasPrecision(3, 1);
        builder.Property(r => r.Comment).HasMaxLength(500);

        builder.HasIndex(r => new { r.MatchId, r.ReviewerParticipantId, r.ReviewedParticipantId })
            .IsUnique();
        builder.HasIndex(r => r.MatchId);
    }
}
