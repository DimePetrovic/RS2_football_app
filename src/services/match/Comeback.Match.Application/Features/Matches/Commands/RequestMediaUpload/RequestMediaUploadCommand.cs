namespace Comeback.Match.Application.Features.Matches.Commands.RequestMediaUpload;

using Comeback.BuildingBlocks.Infrastructure.Media;
using MediatR;

public sealed record RequestMediaUploadCommand(
    Guid MatchId,
    Guid UserId) : IRequest<CloudinaryUploadSignature>;
