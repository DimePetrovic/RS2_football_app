namespace Comeback.Profile.Domain.Events;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record ProfileCreatedDomainEvent(Guid ProfileId, Guid UserId) : IDomainEvent;
