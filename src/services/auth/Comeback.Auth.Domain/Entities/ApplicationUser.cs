namespace Comeback.Auth.Domain.Entities;

using Comeback.Auth.Domain.Enums;
using Microsoft.AspNetCore.Identity;


public sealed class ApplicationUser : IdentityUser<Guid>
{
    public UserRole Role { get; set; }
    public AccountStatus AccountStatus { get; set; } = AccountStatus.PendingEmailVerification;
    public DateTime CreatedAt { get; set; }
}
