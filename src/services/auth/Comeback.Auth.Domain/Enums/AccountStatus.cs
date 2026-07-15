namespace Comeback.Auth.Domain.Enums;

public enum AccountStatus
{
    PendingEmailVerification = 0,
    Active = 1,
    Suspended = 2,
    Deactivated = 3,
}
