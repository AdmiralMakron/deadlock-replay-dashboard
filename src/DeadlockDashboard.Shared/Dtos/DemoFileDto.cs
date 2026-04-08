namespace DeadlockDashboard.Shared.Dtos;

/// <summary>
/// Represents an available demo file.
/// </summary>
public class DemoFileDto
{
    /// <summary>The filename of the demo file.</summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>File size in bytes.</summary>
    public long FileSizeBytes { get; set; }
}
