namespace Comeback.Match.Infrastructure.Persistence.Configurations;

using Comeback.Match.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MatchMediaConfiguration : IEntityTypeConfiguration<MatchMedia>
{
    public void Configure(EntityTypeBuilder<MatchMedia> builder)
    {
        builder.ToTable("match_media");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.UploaderDisplayName).IsRequired().HasMaxLength(100);
        builder.Property(m => m.MediaType).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.StorageProvider).IsRequired().HasMaxLength(50);
        builder.Property(m => m.StoragePublicId).IsRequired().HasMaxLength(300);
        builder.Property(m => m.Url).IsRequired().HasMaxLength(1000);
        builder.Property(m => m.ThumbnailUrl).HasMaxLength(1000);
        builder.Property(m => m.Format).HasMaxLength(20);
        builder.Property(m => m.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(m => m.MatchId);
    }
}
