namespace Comeback.Match.Application.Features.Matches.Commands.DeleteMatchMedia;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.BuildingBlocks.Infrastructure.Media;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class DeleteMatchMediaCommandHandler : IRequestHandler<DeleteMatchMediaCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchMediaRepository _media;
    private readonly IMatchUnitOfWork _unitOfWork;
    private readonly ICloudinaryMediaService _cloudinary;

    public DeleteMatchMediaCommandHandler(
        IMatchRepository matches,
        IMatchMediaRepository media,
        IMatchUnitOfWork unitOfWork,
        ICloudinaryMediaService cloudinary)
    {
        _matches = matches;
        _media = media;
        _unitOfWork = unitOfWork;
        _cloudinary = cloudinary;
    }

    public async Task Handle(DeleteMatchMediaCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        var media = await _media.GetByIdAsync(cmd.MediaId, ct);
        if (media is null || media.MatchId != match.Id || media.Status != MatchMediaStatus.Active)
            throw new NotFoundException("Media not found.", "media.not_found");

        var isUploader = media.UploadedByUserId == cmd.UserId;
        var isOrganizer = match.OrganizerUserId == cmd.UserId || match.SecondOrganizerUserId == cmd.UserId;
        if (!isUploader && !isOrganizer)
            throw new ForbiddenException("Only the author or organizer can delete media.", "media.delete_forbidden");

        media.Remove();
        await _unitOfWork.SaveChangesAsync(ct);

        // Best effort — the metadata is already marked as removed; a failed delete of
        // the file from Cloudinary is only logged inside the service.
        var resourceType = media.MediaType == MatchMediaType.Video ? "video" : "image";
        await _cloudinary.DeleteAsync(media.StoragePublicId, resourceType, ct);
    }
}
