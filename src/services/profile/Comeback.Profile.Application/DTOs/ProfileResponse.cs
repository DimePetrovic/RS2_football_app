namespace Comeback.Profile.Application.DTOs;

public sealed record ProfileResponse(
    Guid Id,
    Guid UserId,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string PreferredPosition,
    bool CanPlayGoalkeeper,
    int YouthSeasons,
    int SeniorSeasons,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? SkillLevel,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string? Nationality);
