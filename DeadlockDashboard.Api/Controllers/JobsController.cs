using DeadlockDashboard.Api.Services;
using DeadlockDashboard.Api.Stores;
using DeadlockDashboard.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeadlockDashboard.Api.Controllers;

/// <summary>Parse-job status endpoints.</summary>
[ApiController]
[Route("api/v1/jobs")]
[Produces("application/json")]
public sealed class JobsController : ControllerBase
{
    private readonly JobStore _jobs;

    public JobsController(JobStore jobs)
    {
        _jobs = jobs;
    }

    /// <summary>Get the status and progress of a parse job.</summary>
    /// <param name="jobId">The job id returned from POST /api/v1/demos/{filename}/parse.</param>
    /// <response code="200">Current job state.</response>
    /// <response code="404">No job with this id.</response>
    [HttpGet("{jobId}", Name = "GetJob")]
    [ProducesResponseType(typeof(ParseJobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status404NotFound)]
    public ActionResult<ParseJobDto> Get(Guid jobId)
    {
        if (!_jobs.TryGet(jobId, out var job) || job is null)
            return NotFound(new ErrorResponseDto("not_found", $"Job '{jobId}' not found."));
        return Ok(job.ToDto());
    }
}
