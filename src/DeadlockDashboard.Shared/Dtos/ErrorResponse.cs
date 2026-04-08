namespace DeadlockDashboard.Shared.Dtos;

/// <summary>
/// Standard error response format for API errors.
/// </summary>
public class ErrorResponse
{
    /// <summary>Machine-readable error code.</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>Human-readable error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Optional additional details about the error.</summary>
    public object? Details { get; set; }
}
