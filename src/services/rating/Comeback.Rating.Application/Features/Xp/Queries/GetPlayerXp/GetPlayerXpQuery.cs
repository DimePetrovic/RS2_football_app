namespace Comeback.Rating.Application.Features.Xp.Queries.GetPlayerXp;

using Comeback.BuildingBlocks.Application.Messaging;
using Comeback.Rating.Application.DTOs;

public sealed record GetPlayerXpQuery(Guid UserId) : IQuery<PlayerXpResponse>;
