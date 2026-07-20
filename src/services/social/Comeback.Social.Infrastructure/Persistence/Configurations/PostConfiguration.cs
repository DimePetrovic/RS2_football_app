namespace Comeback.Social.Infrastructure.Persistence.Configurations;

using Comeback.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(p => p.MatchTitle).IsRequired().HasMaxLength(200);
        builder.Property(p => p.OrganizerDisplayName).HasMaxLength(100);
        builder.Property(p => p.Position).HasMaxLength(30);
        builder.Property(p => p.Location).HasMaxLength(500);

        // One post per (match, type): a match can have both a result post and a player-wanted post.
        builder.HasIndex(p => new { p.MatchId, p.Type }).IsUnique();

        builder.HasMany(p => p.Participants)
            .WithOne()
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Comments)
            .WithOne()
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Comments).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Likes)
            .WithOne()
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Likes).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
