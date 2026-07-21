namespace Comeback.Rating.Domain.Events;

using Comeback.BuildingBlocks.Domain.Events;

public sealed record PlayerXpUpdatedDomainEvent(Guid UserId, int TotalXp, int Level) : IDomainEvent;
