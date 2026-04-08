using System.Collections.Concurrent;
using DeadlockDashboard.Shared;

namespace DeadlockDashboard.Api.Stores;

public sealed class ParseJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Filename { get; init; }
    public JobStatus Status { get; set; } = JobStatus.Pending;
    public int Progress { get; set; }
    public string? MatchId { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Thread-safe in-memory store of parse jobs. Registered as a singleton.
/// </summary>
public sealed class JobStore
{
    private readonly ConcurrentDictionary<Guid, ParseJob> _jobs = new();

    public ParseJob Create(string filename)
    {
        var job = new ParseJob { Filename = filename };
        _jobs[job.Id] = job;
        return job;
    }

    public bool TryGet(Guid id, out ParseJob? job)
    {
        var ok = _jobs.TryGetValue(id, out var j);
        job = j;
        return ok;
    }

    public IReadOnlyCollection<ParseJob> All() => _jobs.Values.ToArray();
}
