namespace Comeback.Auth.Infrastructure.Persistence.Configurations;

using Comeback.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.Id).HasColumnName("id");

        builder.Property(rt => rt.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(rt => rt.Token).HasColumnName("token").IsRequired();
        builder.Property(rt => rt.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(rt => rt.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(rt => rt.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(45).IsRequired();
        builder.Property(rt => rt.RevokedAt).HasColumnName("revoked_at");
        builder.Property(rt => rt.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(45);

        builder.HasIndex(rt => rt.Token).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(rt => rt.IsActive);
    }
}
