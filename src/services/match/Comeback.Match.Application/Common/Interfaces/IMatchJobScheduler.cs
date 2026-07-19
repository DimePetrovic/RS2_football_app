namespace Comeback.Match.Application.Common.Interfaces;

/// <summary>Scheduling/cancelling background jobs related to a match (Hangfire).</summary>
public interface IMatchJobScheduler
{
    /// <summary>Schedules the result-entry reminder and returns the job id (for later cancellation).</summary>
    string ScheduleResultReminder(Guid matchId, DateTimeOffset runAt);

    /// <summary>Cancels a previously scheduled job; safe for a null/non-existent id.</summary>
    void CancelJob(string? jobId);
}
