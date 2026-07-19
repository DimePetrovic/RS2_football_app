namespace Comeback.Match.Infrastructure.Jobs;

using Comeback.Match.Application.Common.Interfaces;
using Hangfire;

public sealed class HangfireMatchJobScheduler : IMatchJobScheduler
{
    private readonly IBackgroundJobClient _jobs;

    public HangfireMatchJobScheduler(IBackgroundJobClient jobs) => _jobs = jobs;

    public string ScheduleResultReminder(Guid matchId, DateTimeOffset runAt)
        => _jobs.Schedule<MatchReminderJob>(j => j.SendResultReminder(matchId), runAt);

    public void CancelJob(string? jobId)
    {
        if (!string.IsNullOrEmpty(jobId))
            _jobs.Delete(jobId);
    }
}
