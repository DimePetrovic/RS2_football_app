namespace Comeback.Rating.Application.Features.Xp.Commands.AwardMatchXp;

using MediatR;

public sealed record AwardMatchXpCommand(Guid MatchId, Guid UserId, int Amount) : IRequest;
