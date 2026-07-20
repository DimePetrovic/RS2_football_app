namespace Comeback.Social.Infrastructure.Persistence.Configurations;

using Comeback.Social.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class PostParticipantConfiguration : IEntityTypeConfiguration<PostParticipant>
{
    public void Configure(EntityTypeBuilder<PostParticipant> builder)
    {
        builder.ToTable("post_participants");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.DisplayName).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.UserId);
    }
}
