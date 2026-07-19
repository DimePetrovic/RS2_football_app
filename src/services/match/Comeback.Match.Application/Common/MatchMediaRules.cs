namespace Comeback.Match.Application.Common;

using Comeback.BuildingBlocks.Domain.Exceptions;
using Comeback.Match.Domain.Entities;
using Comeback.Match.Domain.Enums;

public static class MatchMediaRules
{
    public const long MaxImageSizeInBytes = 10 * 1024 * 1024;
    public const long MaxVideoSizeInBytes = 100 * 1024 * 1024;

    /// <summary>
    /// Only an accepted participant can add media, and only from the match start
    /// (spec: media content is tied to a played match).
    /// </summary>
    public static MatchParticipant EnsureCanManageMedia(Domain.Entities.Match match, Guid userId)
    {
        if (match.Status == MatchStatus.Cancelled)
            throw new BusinessRuleException("The match has been cancelled.", "match.cancelled");

        if (DateTime.UtcNow < match.StartsAt)
            throw new BusinessRuleException("You can add media only after the match starts.", "match.media_after_start");

        return match.Participants.FirstOrDefault(
                p => p.UserId == userId && p.Status == MatchParticipantStatus.Accepted)
            ?? throw new ForbiddenException("Only match participants can add media.", "match.media_participant_only");
    }
}
