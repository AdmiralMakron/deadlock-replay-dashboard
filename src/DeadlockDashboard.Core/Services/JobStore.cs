using System.Collections.Concurrent;
using DeadlockDashboard.Core.Models;

namespace DeadlockDashboard.Core.Services;

public class JobStore
{
    private readonly ConcurrentDictionary<string, ParseJob> _jobs = new();

    public ParseJob CreateJob(string filename)
    {
        var job = new ParseJob
        {
            JobId = Guid.NewGuid().ToString("N")[..12],
            Filename = filename,
            Status = ParseJobStatus.Pending,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };
        _jobs[job.JobId] = job;
        return job;
    }

    public ParseJob? GetJob(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public void UpdateJob(string jobId, Action<ParseJob> update)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            lock (job)
            {
                update(job);
            }
        }
    }
}
