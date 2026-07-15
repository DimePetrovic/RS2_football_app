namespace Comeback.Auth.Domain.Events;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record UserRegisteredDomainEvent(
    Guid UserId,
    string Email,
    string Username,
    string VerificationToken) : IDomainEvent;
