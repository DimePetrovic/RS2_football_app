namespace Comeback.Social.Infrastructure.Http;

using System.Net.Http.Json;
using System.Text.Json;
using Comeback.Social.Application.Common.Interfaces;

public sealed class HttpMatchDetailsClient : IMatchDetailsClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public HttpMatchDetailsClient(HttpClient http) => _http = http;

    public async Task<MatchDetailsInfo?> GetMatchDetailsAsync(Guid matchId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<MatchDetailsResponse>(
                $"/api/matches/internal/{matchId}/details", JsonOptions, ct);
            if (response is null) return null;

            var participants = response.Participants
                .Select(p => new MatchParticipantInfo(p.Id, p.UserId, p.DisplayName, p.Team, p.IsCaptain, p.Status))
                .ToList();
            var goals = response.Goals
                .Select(g => new MatchGoalInfo(g.ScorerUserId, g.ScoringTeam, g.IsOwnGoal, g.AssistUserId))
                .ToList();

            return new MatchDetailsInfo(
                participants, goals,
                response.Location, response.StartsAt,
                response.GroupName, response.OpponentGroupName);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<MatchReviewInfo>> GetReviewsAsync(Guid matchId, CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetFromJsonAsync<List<MatchReviewResponseDto>>(
                $"/api/matches/internal/{matchId}/reviews", JsonOptions, ct);
            if (response is null) return [];

            return response.Select(r => new MatchReviewInfo(
                r.ReviewerParticipantId, r.ReviewedParticipantId, r.OverallRating,
                r.GoalkeepingRating, r.DefenseRating, r.AttackRating, r.EffortRating, r.Comment)).ToList();
        }
        catch
        {
            return [];
        }
    }

    private sealed record MatchDetailsResponse(
        IReadOnlyList<ParticipantResponseDto> Participants,
        IReadOnlyList<GoalResponseDto> Goals,
        string? Location,
        DateTime StartsAt,
        string? GroupName,
        string? OpponentGroupName);

    private sealed record ParticipantResponseDto(Guid Id, Guid UserId, string DisplayName, string Team, bool IsCaptain, string Status);

    private sealed record GoalResponseDto(Guid ScorerUserId, string ScoringTeam, bool IsOwnGoal, Guid? AssistUserId);

    private sealed record MatchReviewResponseDto(
        Guid ReviewerParticipantId,
        Guid ReviewedParticipantId,
        decimal OverallRating,
        decimal? GoalkeepingRating,
        decimal? DefenseRating,
        decimal? AttackRating,
        decimal? EffortRating,
        string? Comment);
}
