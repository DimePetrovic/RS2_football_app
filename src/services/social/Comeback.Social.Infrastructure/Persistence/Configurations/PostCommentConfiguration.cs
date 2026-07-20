namespace Comeback.Social.Infrastructure.Persistence.Configurations;

using Comeback.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PostCommentConfiguration : IEntityTypeConfiguration<PostComment>
{
    public void Configure(EntityTypeBuilder<PostComment> builder)
    {
        builder.ToTable("post_comments");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.AuthorDisplayName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Content).IsRequired().HasMaxLength(1000);
        builder.HasIndex(c => c.PostId);
    }
}
