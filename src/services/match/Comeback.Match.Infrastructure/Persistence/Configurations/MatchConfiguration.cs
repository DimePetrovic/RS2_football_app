namespace Comeback.Match.Infrastructure.Persistence.Configurations;

using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Domain.Entities.Match>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Match> builder)
    {
        builder.ToTable("matches");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Title).IsRequired().HasMaxLength(200);
        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(m => m.Location).HasMaxLength(500);
        builder.Property(m => m.ResultReminderJobId).HasMaxLength(100);
        builder.Ignore(m => m.EndsAt);
        builder.Ignore(m => m.HasResult);

        builder.Property(m => m.GroupName).HasMaxLength(100);
        builder.Property(m => m.OpponentGroupName).HasMaxLength(100);
        builder.Property(m => m.OpponentGroupCaptainDisplayName).HasMaxLength(100);
        builder.Property(m => m.OpponentGroupInviteStatus).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(m => m.GroupId);
        builder.HasIndex(m => m.OpponentGroupId);

        builder.HasMany(m => m.Participants)
            .WithOne()
            .HasForeignKey(p => p.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(m => m.Goals)
            .WithOne()
            .HasForeignKey(g => g.MatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(m => m.Goals)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
