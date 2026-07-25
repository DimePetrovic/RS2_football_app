namespace Comeback.Social.Application.Common;

using Comeback.Social.Application.Common.Interfaces;
using Comeback.Social.Application.DTOs;
using Comeback.Social.Domain.Enums;
using Comeback.Social.Domain.Entities;

public sealed class PostEnricher
{
    private readonly IMatchDetailsClient _matchClient;
    private readonly IProfileAvatarsClient _avatarsClient;

    public PostEnricher(IMatchDetailsClient matchClient, IProfileAvatarsClient avatarsClient)
    {
        _matchClient = matchClient;
        _avatarsClient = avatarsClient;
    }

    public async Task<PostResponse> EnrichAsync(Post post, Guid currentUserId, CancellationToken ct)
    {
        if (post.Type == PostType.PlayerWanted)
        {
            // The organizer badge and the live participation marker come from two independent
            // services — fetch them concurrently.
            var organizerTask = _avatarsClient.GetPlayerInfosAsync([post.OrganizerUserId], ct);
            var detailsTask = _matchClient.GetMatchDetailsAsync(post.MatchId, ct);
            await Task.WhenAll(organizerTask, detailsTask);

            var organizer = (await organizerTask).GetValueOrDefault(post.OrganizerUserId);
            var organizerName = string.IsNullOrWhiteSpace(organizer?.DisplayName)
                ? post.OrganizerDisplayName
                : organizer!.DisplayName!;

            var details = await detailsTask;
            var viewerAlreadyIn = currentUserId == post.OrganizerUserId
                || details?.Participants.Any(p =>
                    p.UserId == currentUserId && p.Status is "Invited" or "Accepted") == true;

            return new PostResponse(
                post.Id, post.Type.ToString(), post.MatchId, post.MatchTitle, 0, 0,
                post.Location, post.StartsAt, null, null, post.CreatedAt,
                0, false, 0, CanInteract: false,
                post.OrganizerUserId, organizerName, organizer?.Username, organizer?.AvatarUrl, organizer?.Nationality,
                post.Position, viewerAlreadyIn, []);
        }

        var matchDetails = await _matchClient.GetMatchDetailsAsync(post.MatchId, ct);
        var players = matchDetails is null
            ? []
            : await BuildPlayersAsync(post, matchDetails, ct);

        return new PostResponse(
            post.Id,
            post.Type.ToString(),
            post.MatchId,
            post.MatchTitle,
            post.HomeScore,
            post.AwayScore,
            matchDetails?.Location,
            matchDetails?.StartsAt,
            matchDetails?.GroupName,
            matchDetails?.OpponentGroupName,
            post.CreatedAt,
            post.Likes.Count,
            post.Likes.Any(l => l.UserId == currentUserId),
            post.Comments.Count,
            post.CanInteract,
            OrganizerUserId: null,
            OrganizerDisplayName: null,
            OrganizerUsername: null,
            OrganizerAvatarUrl: null,
            OrganizerNationality: null,
            Position: null,
            ViewerAlreadyIn: false,
            players);
    }

    private async Task<List<PostPlayerDto>> BuildPlayersAsync(Post post, MatchDetailsInfo matchDetails, CancellationToken ct)
    {
        // Reviews and player avatars depend only on data already in hand — fetch them concurrently.
        var reviewsTask = _matchClient.GetReviewsAsync(post.MatchId, ct);
        var playerInfosTask = _avatarsClient.GetPlayerInfosAsync(
            matchDetails.Participants.Where(p => p.Team is "Home" or "Away").Select(p => p.UserId), ct);
        await Task.WhenAll(reviewsTask, playerInfosTask);

        var reviews = await reviewsTask;
        var playerInfos = await playerInfosTask;
        string ResolveName(Guid userId, string fallback)
        {
            var i = playerInfos.GetValueOrDefault(userId);
            return string.IsNullOrWhiteSpace(i?.DisplayName) ? fallback : i!.DisplayName!;
        }

        var displayNameByParticipantId = matchDetails.Participants
            .ToDictionary(p => p.ParticipantId, p => ResolveName(p.UserId, p.DisplayName));

        var players = new List<PostPlayerDto>();
        foreach (var p in matchDetails.Participants.Where(p => p.Team is "Home" or "Away"))
        {
            var goals = matchDetails.Goals.Count(g => g.ScorerUserId == p.UserId && !g.IsOwnGoal);
            var ownGoals = matchDetails.Goals.Count(g => g.ScorerUserId == p.UserId && g.IsOwnGoal);
            var assists = matchDetails.Goals.Count(g => g.AssistUserId == p.UserId);

            var playerReviews = reviews.Where(r => r.ReviewedParticipantId == p.ParticipantId).ToList();
            decimal? Avg(Func<MatchReviewInfo, decimal?> sel)
            {
                var values = playerReviews.Select(sel).Where(v => v.HasValue).Select(v => v!.Value).ToList();
                return values.Count == 0 ? null : Math.Round(values.Average(), 1);
            }

            var comments = playerReviews
                .Where(r => !string.IsNullOrWhiteSpace(r.Comment))
                .Select(r => new PlayerCommentDto(
                    displayNameByParticipantId.GetValueOrDefault(r.ReviewerParticipantId, "Unknown player"),
                    r.Comment!))
                .ToList();

            var info = playerInfos.GetValueOrDefault(p.UserId);
            players.Add(new PostPlayerDto(
                p.UserId, ResolveName(p.UserId, p.DisplayName), info?.Username, info?.AvatarUrl, info?.Nationality, p.Team, p.IsCaptain,
                goals, assists, ownGoals,
                Avg(r => (decimal?)r.OverallRating),
                Avg(r => r.GoalkeepingRating),
                Avg(r => r.DefenseRating),
                Avg(r => r.AttackRating),
                Avg(r => r.EffortRating),
                comments));
        }

        return players;
    }
}
