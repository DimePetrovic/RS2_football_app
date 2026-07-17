namespace Comeback.Profile.Application.DTOs;

public sealed record AdminUserListItem(
    Guid UserId,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    DateTime CreatedAt);
