namespace DeadlockDashboard.Core.Models;

public class ParseJob
{
    public string JobId { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public ParseJobStatus Status { get; set; } = ParseJobStatus.Pending;
    public int Progress { get; set; }
    public string? MatchId { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum ParseJobStatus
{
    Pending,
    Running,
    Completed,
    Failed
}
