namespace DeadlockDashboard.Shared.Dtos;

/// <summary>
/// Represents the status of a demo parse job.
/// </summary>
public class ParseJobDto
{
    /// <summary>Unique job identifier.</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>Current job status: Pending, Running, Completed, or Failed.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Parse progress from 0 to 100.</summary>
    public int Progress { get; set; }

    /// <summary>The resulting match ID if the job completed successfully.</summary>
    public string? MatchId { get; set; }

    /// <summary>Error message if the job failed.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Response returned when a parse job is created.
/// </summary>
public class ParseJobCreatedDto
{
    /// <summary>The ID of the created parse job.</summary>
    public string JobId { get; set; } = string.Empty;
}
