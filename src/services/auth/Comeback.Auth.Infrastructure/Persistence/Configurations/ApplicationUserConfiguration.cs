namespace Comeback.Auth.Infrastructure.Persistence.Configurations;

using Comeback.Auth.Domain.Entities;
using Comeback.Auth.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasDefaultValue(UserRole.Player)
            .IsRequired();

        builder.Property(u => u.AccountStatus)
            .HasConversion<string>()
            .HasDefaultValue(AccountStatus.PendingEmailVerification)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();
    }
}
