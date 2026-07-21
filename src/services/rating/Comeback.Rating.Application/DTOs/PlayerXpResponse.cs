namespace Comeback.Rating.Application.DTOs;

public sealed record PlayerXpResponse(
    Guid UserId,
    int TotalXp,
    int Level,
    int CareerXp,
    int MatchXp,
    int YouthSeasons,
    int SeniorSeasons,
    int XpToNextLevel,
    DateTime UpdatedAt);
