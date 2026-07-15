namespace Comeback.BuildingBlocks.IntegrationEvents.Auth;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record UserEmailConfirmedIntegrationEvent(
    Guid UserId,
    string Email,
    string Username,
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    int PreferredPosition,
    bool CanPlayGoalkeeper,
    int YouthSeasons,
    int SeniorSeasons,
    string Role,
    string? Nationality) : IntegrationEvent;
