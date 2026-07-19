namespace Comeback.Match.Application.Features.Matches.Commands.SubmitReview;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Match.Application.Common.Interfaces;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;
using MediatR;

public sealed class SubmitReviewCommandHandler : IRequestHandler<SubmitReviewCommand>
{
    private readonly IMatchRepository _matches;
    private readonly IMatchReviewRepository _reviews;
    private readonly IMatchUnitOfWork _unitOfWork;

    public SubmitReviewCommandHandler(
        IMatchRepository matches,
        IMatchReviewRepository reviews,
        IMatchUnitOfWork unitOfWork)
    {
        _matches = matches;
        _reviews = reviews;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(SubmitReviewCommand cmd, CancellationToken ct)
    {
        var match = await _matches.GetByIdWithParticipantsAsync(cmd.MatchId, ct)
            ?? throw new NotFoundException("Match not found.", "match.not_found");

        if (match.Status != MatchStatus.ResultSubmitted)
            throw new BusinessRuleException("Reviews are available only after the result is entered.", "review.after_result_only");

        if (cmd.OverallRating < 5.0m || cmd.OverallRating > 10.0m
            || Math.Round(cmd.OverallRating * 2) != cmd.OverallRating * 2)
            throw new BusinessRuleException("The rating must be between 5 and 10 in steps of 0.5.", "review.rating_range");

        var reviewer = match.Participants
            .FirstOrDefault(p => p.UserId == cmd.ReviewerUserId && p.Team != MatchTeam.None)
            ?? throw new BusinessRuleException("Only players assigned to a team can submit reviews.", "review.reviewer_no_team");

        var reviewed = match.Participants
            .FirstOrDefault(p => p.Id == cmd.ReviewedParticipantId && p.Team != MatchTeam.None)
            ?? throw new NotFoundException("The reviewed player is not assigned to a team.", "review.reviewed_no_team");

        if (reviewer.Id == reviewed.Id)
            throw new BusinessRuleException("You cannot review yourself.", "review.self");

        if (reviewed.IsGuest)
            throw new BusinessRuleException("Guests without an account cannot be reviewed.", "review.guest");

        var existing = await _reviews.GetAsync(match.Id, reviewer.Id, reviewed.Id, ct);
        if (existing is not null)
        {
            existing.Update(cmd.OverallRating,
                cmd.GoalkeepingRating, cmd.DefenseRating,
                cmd.AttackRating, cmd.EffortRating, cmd.Comment);
        }
        else
        {
            _reviews.Add(MatchPlayerReview.Create(
                match.Id, reviewer.Id, reviewed.Id, cmd.OverallRating,
                cmd.GoalkeepingRating, cmd.DefenseRating,
                cmd.AttackRating, cmd.EffortRating, cmd.Comment));
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
