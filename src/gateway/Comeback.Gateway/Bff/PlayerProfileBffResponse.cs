namespace Comeback.Gateway.Bff;

public sealed record PlayerProfileBffResponse(
    ProfileData Profile,
    PlayerXpData? Rating);

public sealed record ProfileData(
    string UserId,
    string FirstName,
    string LastName,
    string? DisplayName,
    string? Bio,
    string? AvatarUrl,
    string? PreferredPosition,
    bool CanPlayGoalkeeper,
    int YouthSeasons,
    int SeniorSeasons,
    string? SkillLevel,
    string CreatedAt);

public sealed record PlayerXpData(
    int TotalXp,
    int Level,
    int CareerXp,
    int MatchXp,
    int YouthSeasons,
    int SeniorSeasons,
    int XpToNextLevel,
    string UpdatedAt);
