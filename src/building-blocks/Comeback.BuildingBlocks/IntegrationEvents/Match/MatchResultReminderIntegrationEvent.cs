namespace Comeback.BuildingBlocks.IntegrationEvents.Match;

using Comeback.BuildingBlocks.Domain.Events;

/// <summary>Reminder to the organizer to enter the result (15min after the match ends).</summary>
public sealed record MatchResultReminderIntegrationEvent(
    Guid MatchId,
    string MatchTitle,
    Guid OrganizerUserId) : IntegrationEvent;
