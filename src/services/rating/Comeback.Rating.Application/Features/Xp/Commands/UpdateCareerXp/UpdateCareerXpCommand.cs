namespace Comeback.Rating.Application.Features.Xp.Commands.UpdateCareerXp;

using Comeback.BuildingBlocks.Application.Messaging;

public sealed record UpdateCareerXpCommand(
    Guid UserId,
    int YouthSeasons,
    int SeniorSeasons) : ICommand;
