namespace Comeback.BuildingBlocks.IntegrationEvents.Auth;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record UserRegisteredIntegrationEvent(
    Guid UserId,
    string Email,
    string Username) : IntegrationEvent;
