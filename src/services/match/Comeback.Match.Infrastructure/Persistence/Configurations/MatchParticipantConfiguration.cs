namespace Comeback.Match.Infrastructure.Persistence.Configurations;

using Comeback.Match.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MatchParticipantConfiguration : IEntityTypeConfiguration<MatchParticipant>
{
    public void Configure(EntityTypeBuilder<MatchParticipant> builder)
    {
        builder.ToTable("match_participants");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(p => p.Team).HasConversion<string>().HasMaxLength(20).HasDefaultValue(Comeback.Match.Domain.Enums.MatchTeam.None);
        builder.Property(p => p.GroupSide).HasConversion<string>().HasMaxLength(20).HasDefaultValue(Comeback.Match.Domain.Enums.MatchTeam.None);

        builder.HasIndex(p => new { p.MatchId, p.UserId }).IsUnique();
        builder.HasIndex(p => p.UserId);
    }
}
