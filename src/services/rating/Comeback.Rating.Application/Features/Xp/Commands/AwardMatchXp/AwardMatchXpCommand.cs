namespace Comeback.Rating.Application.Features.Xp.Commands.AwardMatchXp;

using MediatR;

public sealed record AwardMatchXpCommand(Guid UserId, int Amount) : IRequest;
