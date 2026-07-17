namespace Comeback.BuildingBlocks.IntegrationEvents.Profile;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record PlayerCareerDataUpdatedIntegrationEvent(
    Guid UserId,
    int YouthSeasons,
    int SeniorSeasons) : IntegrationEvent;
