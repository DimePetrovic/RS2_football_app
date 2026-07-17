namespace Comeback.Profile.Application.DTOs;

public sealed record ProfileSearchResult(
    Guid UserId,
    string Username,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? AvatarUrl,
    string? Nationality);
