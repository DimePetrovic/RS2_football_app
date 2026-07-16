namespace Comeback.Match.Infrastructure.Persistence.Configurations;

using Comeback.Match.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MatchGoalConfiguration : IEntityTypeConfiguration<MatchGoal>
{
    public void Configure(EntityTypeBuilder<MatchGoal> builder)
    {
        builder.ToTable("match_goals");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id).ValueGeneratedNever();

        builder.Property(g => g.ScorerDisplayName).IsRequired().HasMaxLength(100);
        builder.Property(g => g.AssistDisplayName).HasMaxLength(100);
        builder.Property(g => g.ScoringTeam).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(g => g.MatchId);
    }
}
