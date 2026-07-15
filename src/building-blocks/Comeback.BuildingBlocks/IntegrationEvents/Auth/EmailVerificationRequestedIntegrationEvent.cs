namespace Comeback.BuildingBlocks.IntegrationEvents.Auth;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record EmailVerificationRequestedIntegrationEvent(
    Guid UserId,
    string Email,
    string Username,
    string VerificationToken) : IntegrationEvent;
