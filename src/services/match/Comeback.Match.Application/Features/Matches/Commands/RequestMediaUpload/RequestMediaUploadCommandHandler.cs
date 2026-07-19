namespace Comeback.Match.Application.Features.Matches.Commands.RequestMediaUpload;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.Infrastructure.Media;
using Comeback.Match.Application.Common;
using Comeback.Match.Application.Common.Interfaces;
using MediatR;

public sealed class RequestMediaUploadCommandHandler
    : IRequestHandler<RequestMediaUploadCommand, CloudinaryUploadSignature>
{
    private readonly IMatchRepository _matches;
    private readonly ICloudinaryMediaService _cloudinary;

    public RequestMediaUploadCommandHandler(IMatchRepository matches, ICloudinaryMediaService cloudinary)
    {
        _matches = matches;
        _cloudinary = cloudinary;
    }

    public async Task<CloudinaryUploadSignature> Handle(RequestMediaUploadCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        MatchMediaRules.EnsureCanManageMedia(match, cmd.UserId);

        return _cloudinary.CreateUploadSignature($"comeback/matches/{cmd.MatchId}");
    }
}
