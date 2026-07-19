namespace Comeback.Match.Application.Features.Matches.Commands.AddMatchMedia;

using Comeback.Match.Application.DTOs;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed record AddMatchMediaCommand(
    Guid MatchId,
    Guid UserId,
    MatchMediaType MediaType,
    string StoragePublicId,
    string Url,
    string? ThumbnailUrl,
    string? Format,
    long? SizeInBytes,
    double? DurationInSeconds,
    int? Width,
    int? Height) : IRequest<MatchMediaResponse>;
