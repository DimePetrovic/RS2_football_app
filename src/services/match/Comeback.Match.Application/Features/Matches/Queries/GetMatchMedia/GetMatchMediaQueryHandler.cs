namespace Comeback.Match.Application.Features.Matches.Queries.GetMatchMedia;

using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using MediatR;

public sealed class GetMatchMediaQueryHandler
    : IRequestHandler<GetMatchMediaQuery, IReadOnlyList<MatchMediaResponse>>
{
    private readonly IMatchMediaRepository _media;

    public GetMatchMediaQueryHandler(IMatchMediaRepository media)
        => _media = media;

    public async Task<IReadOnlyList<MatchMediaResponse>> Handle(GetMatchMediaQuery query, CancellationToken ct)
    {
        var items = await _media.GetActiveByMatchAsync(query.MatchId, ct);
        return items.Select(m => new MatchMediaResponse(
            m.Id, m.UploadedByUserId, m.UploaderDisplayName,
            m.MediaType.ToString(), m.Url, m.ThumbnailUrl,
            m.Format, m.SizeInBytes, m.DurationInSeconds,
            m.Width, m.Height, m.CreatedAt)).ToList();
    }
}
