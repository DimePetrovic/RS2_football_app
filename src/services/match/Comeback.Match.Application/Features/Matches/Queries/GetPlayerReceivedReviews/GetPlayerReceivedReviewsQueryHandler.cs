namespace Comeback.Match.Application.Features.Matches.Queries.GetPlayerReceivedReviews;

using Comeback.BuildingBlocks.Application.Clients;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Application.DTOs;
using MediatR;

public sealed class GetPlayerReceivedReviewsQueryHandler
    : IRequestHandler<GetPlayerReceivedReviewsQuery, IReadOnlyList<PlayerReceivedReviewItem>>
{
    private readonly IMatchReviewRepository _reviews;
    private readonly IPlayerInfoClient _playerInfo;

    public GetPlayerReceivedReviewsQueryHandler(
        IMatchReviewRepository reviews, IPlayerInfoClient playerInfo)
    {
        _reviews = reviews;
        _playerInfo = playerInfo;
    }

    public async Task<IReadOnlyList<PlayerReceivedReviewItem>> Handle(
        GetPlayerReceivedReviewsQuery query, CancellationToken ct)
    {
        var items = await _reviews.GetReceivedByUserAsync(query.UserId, ct);
        if (items.Count == 0) return items;

        // Reviewers are shown with avatar and username — enrichment from the Profile service.
        var infos = (await _playerInfo.GetPlayerInfosAsync(
                items.Select(i => i.ReviewerUserId).Distinct(), ct))
            .ToDictionary(i => i.UserId);

        return items
            .Select(i => infos.TryGetValue(i.ReviewerUserId, out var info)
                ? i with { ReviewerUsername = info.Username, ReviewerAvatarUrl = info.AvatarUrl, ReviewerNationality = info.Nationality }
                : i)
            .ToList();
    }
}
