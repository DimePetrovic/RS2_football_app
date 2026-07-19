namespace Comeback.Match.Application.Features.Matches.Commands.AddMatchMedia;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Match.Application.Common;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class AddMatchMediaCommandHandler : IRequestHandler<AddMatchMediaCommand, MatchMediaResponse>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchMediaRepository _media;
    private readonly IMatchUnitOfWork _unitOfWork;

    public AddMatchMediaCommandHandler(
        IMatchRepository matches,
        IMatchMediaRepository media,
        IMatchUnitOfWork unitOfWork)
    {
        _matches = matches;
        _media = media;
        _unitOfWork = unitOfWork;
    }

    public async Task<MatchMediaResponse> Handle(AddMatchMediaCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        var participant = MatchMediaRules.EnsureCanManageMedia(match, cmd.UserId);

        if (string.IsNullOrWhiteSpace(cmd.StoragePublicId) || string.IsNullOrWhiteSpace(cmd.Url))
            throw new BusinessRuleException("Media must have a storage id and file URL.", "media.missing_fields");

        if (!cmd.Url.StartsWith("https://res.cloudinary.com/", StringComparison.OrdinalIgnoreCase))
            throw new BusinessRuleException("The file URL must be from Cloudinary storage.", "media.url_not_cloudinary");

        var maxSize = cmd.MediaType == MatchMediaType.Video
            ? MatchMediaRules.MaxVideoSizeInBytes
            : MatchMediaRules.MaxImageSizeInBytes;
        if (cmd.SizeInBytes > maxSize)
            throw new BusinessRuleException("The file is larger than allowed.", "media.file_too_large");

        var media = MatchMedia.Create(
            match.Id, participant.UserId, participant.DisplayName,
            cmd.MediaType, cmd.StoragePublicId, cmd.Url, cmd.ThumbnailUrl,
            cmd.Format, cmd.SizeInBytes, cmd.DurationInSeconds, cmd.Width, cmd.Height);

        _media.Add(media);
        await _unitOfWork.SaveChangesAsync(ct);

        return new MatchMediaResponse(
            media.Id, media.UploadedByUserId, media.UploaderDisplayName,
            media.MediaType.ToString(), media.Url, media.ThumbnailUrl,
            media.Format, media.SizeInBytes, media.DurationInSeconds,
            media.Width, media.Height, media.CreatedAt);
    }
}
