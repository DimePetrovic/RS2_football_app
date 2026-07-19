namespace Comeback.Match.Application.Features.Matches.Commands.SubmitReview;

using MediatR;

public sealed record SubmitReviewCommand(
    Guid MatchId,
    Guid ReviewerUserId,
    Guid ReviewedParticipantId,
    decimal OverallRating,
    decimal? GoalkeepingRating,
    decimal? DefenseRating,
    decimal? AttackRating,
    decimal? EffortRating,
    string? Comment) : IRequest;
