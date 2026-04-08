using DeadlockDashboard.Core.Services;
using DeadlockDashboard.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DeadlockDashboard.Api.Controllers;

/// <summary>
/// Monitors the status of demo parse jobs.
/// </summary>
[ApiController]
[Route("api/v1/jobs")]
[Produces("application/json")]
public class JobsController : ControllerBase
{
    private readonly JobStore _jobStore;

    public JobsController(JobStore jobStore)
    {
        _jobStore = jobStore;
    }

    /// <summary>
    /// Returns the current status of a parse job.
    /// </summary>
    /// <param name="jobId">The job ID returned from the parse endpoint.</param>
    /// <returns>Job status including progress, match ID, or error message.</returns>
    /// <response code="200">Job status returned successfully.</response>
    /// <response code="404">Job not found.</response>
    [HttpGet("{jobId}")]
    [ProducesResponseType(typeof(ParseJobDto), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    public IActionResult GetJobStatus(string jobId)
    {
        var job = _jobStore.GetJob(jobId);
        if (job == null)
        {
            return NotFound(new ErrorResponse
            {
                Error = "JOB_NOT_FOUND",
                Message = $"Job '{jobId}' was not found."
            });
        }

        return Ok(new ParseJobDto
        {
            JobId = job.JobId,
            Status = job.Status.ToString(),
            Progress = job.Progress,
            MatchId = job.MatchId,
            ErrorMessage = job.ErrorMessage
        });
    }
}
